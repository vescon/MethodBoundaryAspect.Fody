using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// MethodExecutionArgs properties which are not used by any aspect of a method are not provided,
    /// see <see cref="ExecutionArgsUsageAnalyzer"/>.
    /// </summary>
    [Collection(RefStructWarningTests.TestAssemblyNetCoreWeaving)]
    public class ExecutionArgsUsageTests : IDisposable
    {
        private const ExecutionArgsUsage All = ExecutionArgsUsage.All;

        private static readonly string[] OptimizedAccessors =
            { "set_Arguments", "set_Method", "set_Instance", "set_ReturnValue", "get_ReturnValue" };

        private readonly List<string> _weavedAssemblyPaths = new List<string>();

        public static IEnumerable<object[]> AspectUsages => new[]
        {
            new object[] { typeof(NoUsageAspect), ExecutionArgsUsage.None },
            new object[] { typeof(FlowBehaviorUsageAspect), ExecutionArgsUsage.None },
            new object[] { typeof(MethodUsageAspect), ExecutionArgsUsage.Method },
            new object[] { typeof(ArgumentsUsageAspect), ExecutionArgsUsage.Arguments },
            new object[] { typeof(ReadReturnValueUsageAspect), ExecutionArgsUsage.ReadReturnValue },
            new object[] { typeof(WriteReturnValueUsageAspect), ExecutionArgsUsage.WriteReturnValue },
            new object[] { typeof(InstanceUsageAspect), ExecutionArgsUsage.Instance },
            new object[] { typeof(BaseCallUsageAspect), ExecutionArgsUsage.Method | ExecutionArgsUsage.Arguments },
            new object[] { typeof(HelperUsageAspect), ExecutionArgsUsage.Method | ExecutionArgsUsage.Instance },
            new object[] { typeof(RecursiveHelperUsageAspect), ExecutionArgsUsage.Arguments },
            new object[] { typeof(NullConditionalUsageAspect), ExecutionArgsUsage.Method },
            new object[] { typeof(ExternalMethodUsageAspect), All },
            new object[] { typeof(VirtualHelperUsageAspect), All },
            new object[] { typeof(LambdaUsageAspect), All },
            new object[] { typeof(FieldUsageAspect), All },
            new object[] { typeof(TagUsageAspect), All },
            new object[] { typeof(ToStringUsageAspect), All },
            new object[] { typeof(ReassignedParameterUsageAspect), All },
        };

        [Theory]
        [MemberData(nameof(AspectUsages))]
        public void IfAspectIsAnalyzed_ThenTheUsedPropertiesAreFound(Type aspectType, ExecutionArgsUsage expectedUsage)
        {
            var analyzer = new ExecutionArgsUsageAnalyzer(false, Enumerable.Empty<string>());

            var usage = analyzer.GetUsage(ResolveType(aspectType));

            usage.Should().Be(expectedUsage);
        }

        [Fact]
        public void IfOptimizationIsDisabled_ThenAllPropertiesAreUsed()
        {
            var analyzer = new ExecutionArgsUsageAnalyzer(true, Enumerable.Empty<string>());

            analyzer.GetUsage(ResolveType(typeof(NoUsageAspect))).Should().Be(All);
        }

        [Fact]
        public void IfOptimizationIsDisabledForAnAspect_ThenOnlyThisAspectUsesAllProperties()
        {
            var analyzer = new ExecutionArgsUsageAnalyzer(false, new[] { typeof(OptOutUsageAspect).FullName });

            analyzer.GetUsage(ResolveType(typeof(OptOutUsageAspect))).Should().Be(All);
            analyzer.GetUsage(ResolveType(typeof(NoUsageAspect))).Should().Be(ExecutionArgsUsage.None);
            analyzer.MatchedDisabledAspects.Should().Equal(typeof(OptOutUsageAspect).FullName);
        }

        [Fact]
        public void IfAspectIsFromAReferenceAssembly_ThenAllPropertiesAreUsed()
        {
            var aspectType = ResolveType(typeof(NoUsageAspect));
            var referenceAssemblyAttributeCtor = aspectType.Module.ImportReference(
                typeof(System.Runtime.CompilerServices.ReferenceAssemblyAttribute).GetConstructor(Type.EmptyTypes));
            aspectType.Module.Assembly.CustomAttributes.Add(new CustomAttribute(referenceAssemblyAttributeCtor));

            var analyzer = new ExecutionArgsUsageAnalyzer(false, Enumerable.Empty<string>());

            analyzer.GetUsage(aspectType).Should().Be(All);
        }

        [Fact]
        public void IfAspectMethodThrowsOnly_ThenAllPropertiesAreUsed()
        {
            // method bodies of reference assemblies are "throw null"
            var aspectType = ResolveType(typeof(NoUsageAspect));
            var onEntry = aspectType.Methods.Single(m => m.Name == "OnEntry");
            onEntry.Body.Instructions.Clear();
            onEntry.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            onEntry.Body.Instructions.Add(Instruction.Create(OpCodes.Throw));

            var analyzer = new ExecutionArgsUsageAnalyzer(false, Enumerable.Empty<string>());

            analyzer.GetUsage(aspectType).Should().Be(All);
        }

        [Theory]
        [InlineData(nameof(ExecutionArgsUsageMethods.NoUsage))]
        [InlineData(nameof(ExecutionArgsUsageMethods.MethodUsage), "set_Method")]
        [InlineData(nameof(ExecutionArgsUsageMethods.ArgumentsUsage), "set_Arguments")]
        [InlineData(nameof(ExecutionArgsUsageMethods.ReadReturnValueUsage), "set_ReturnValue")]
        // the return value is provided because it's read back after OnExit, also if the aspect doesn't overwrite it
        [InlineData(nameof(ExecutionArgsUsageMethods.WriteReturnValueUsage), "set_ReturnValue", "get_ReturnValue")]
        [InlineData(nameof(ExecutionArgsUsageMethods.InstanceUsage), "set_Instance")]
        [InlineData(nameof(ExecutionArgsUsageMethods.CombinedUsage), "set_Arguments", "set_Method")]
        // OnEntry only: the return value is only read back if the method body is skipped (FlowBehavior.Return)
        [InlineData(nameof(ExecutionArgsUsageMethods.EscapingUsage), "set_Arguments", "set_Method", "set_Instance", "get_ReturnValue")]
        [InlineData(nameof(ExecutionArgsUsageMethods.OptOutUsage), "set_Arguments", "set_Method", "set_Instance", "get_ReturnValue")]
        public void IfMethodIsWeaved_ThenOnlyTheUsedPropertiesAreProvided(string methodName, params string[] expectedAccessors)
        {
            var weavedModule = Weave(OptOutConfig(), out _);

            var accessors = GetExecutionArgsAccessors(weavedModule, methodName);

            accessors.Should().BeEquivalentTo(expectedAccessors);
            HasArgumentsArray(weavedModule, methodName).Should().Be(expectedAccessors.Contains("set_Arguments"));
        }

        [Fact]
        public void IfOptimizationIsDisabledInConfig_ThenAllPropertiesAreProvided()
        {
            var weavedModule = Weave(new XElement("MethodBoundaryAspect", new XAttribute("DisableExecutionArgsOptimization", "true")), out _);

            GetExecutionArgsAccessors(weavedModule, nameof(ExecutionArgsUsageMethods.NoUsage))
                .Should().BeEquivalentTo("set_Arguments", "set_Method", "set_Instance", "get_ReturnValue");
        }

        [Fact]
        public void IfOptimizationIsNotConfigured_ThenItIsEnabled()
        {
            var weavedModule = Weave(null, out _);

            GetExecutionArgsAccessors(weavedModule, nameof(ExecutionArgsUsageMethods.OptOutUsage)).Should().BeEmpty();
        }

        [Fact]
        public void IfOptimizationIsDisabledForAnAspectInConfig_ThenNoWarningIsWritten()
        {
            Weave(OptOutConfig(), out var warnings);

            warnings.Should().BeEmpty();
        }

        [Fact]
        public void IfUnknownAspectIsConfigured_ThenAWarningIsWritten()
        {
            var config = new XElement("MethodBoundaryAspect",
                new XElement("DisableExecutionArgsOptimization", new XAttribute("Aspect", "Unknown.Aspect")));

            Weave(config, out var warnings);

            warnings.Should().ContainSingle(w => w.Contains("'Unknown.Aspect'") && w.Contains("DisableExecutionArgsOptimization"));
        }

        [Fact]
        public void IfConfiguredAspectHasNoName_ThenWeavingFails()
        {
            var config = new XElement("MethodBoundaryAspect", new XElement("DisableExecutionArgsOptimization"));

            var weaver = new ModuleWeaver
            {
                Config = config,
                ModuleDefinition = ModuleDefinition.ReadModule(typeof(ExecutionArgsUsageMethods).Assembly.Location,
                    new ReaderParameters { AssemblyResolver = ModuleHelper.AssemblyResolver })
            };

            Action weave = () => weaver.Execute();

            weave.Should().Throw<Exception>().Where(e => e.Message.Contains("DisableExecutionArgsOptimization"));
        }

        private static XElement OptOutConfig() =>
            new XElement("MethodBoundaryAspect",
                new XElement("DisableExecutionArgsOptimization", new XAttribute("Aspect", typeof(OptOutUsageAspect).FullName)));

        private static TypeDefinition ResolveType(Type type)
        {
            var module = ModuleDefinition.ReadModule(type.Assembly.Location, new ReaderParameters { AssemblyResolver = ModuleHelper.AssemblyResolver });
            return module.GetType(type.FullName);
        }

        private static string[] GetExecutionArgsAccessors(ModuleDefinition module, string methodName)
        {
            return GetWeavedMethod(module, methodName).Body.Instructions
                .Select(i => i.Operand)
                .OfType<MethodReference>()
                .Where(m => m.DeclaringType.Name == "MethodExecutionArgs")
                .Select(m => m.Name)
                .Where(OptimizedAccessors.Contains)
                .Distinct()
                .ToArray();
        }

        private static bool HasArgumentsArray(ModuleDefinition module, string methodName)
        {
            return GetWeavedMethod(module, methodName).Body.Instructions.Any(i => i.OpCode == OpCodes.Newarr);
        }

        private static MethodDefinition GetWeavedMethod(ModuleDefinition module, string methodName)
        {
            return module.GetType(typeof(ExecutionArgsUsageMethods).FullName).Methods.Single(m => m.Name == methodName);
        }

        private ModuleDefinition Weave(XElement config, out List<string> warnings)
        {
            var loggedWarnings = new List<string>();
            // Fody sets these (for consumers obsolete) callbacks, they are the only way to capture the warnings
#pragma warning disable CS0618
            var weaver = new ModuleWeaver
            {
                Config = config,
                LogWarning = loggedWarnings.Add,
                LogWarningPoint = (message, _) => loggedWarnings.Add(message)
            };
#pragma warning restore CS0618
            weaver.AddClassFilter(typeof(ExecutionArgsUsageMethods).FullName);

            var assemblyPath = typeof(ExecutionArgsUsageMethods).Assembly.Location;
            var weavedAssemblyPath = weaver.WeaveToShadowFile(assemblyPath, ModuleHelper.AssemblyResolver);
            _weavedAssemblyPaths.Add(weavedAssemblyPath);

            warnings = loggedWarnings;
            return ModuleDefinition.ReadModule(new MemoryStream(File.ReadAllBytes(weavedAssemblyPath)),
                new ReaderParameters { AssemblyResolver = ModuleHelper.AssemblyResolver });
        }

        public void Dispose()
        {
            foreach (var path in _weavedAssemblyPaths)
            {
                File.Delete(path);
                File.Delete(Path.ChangeExtension(path, "pdb"));
            }
        }
    }
}
