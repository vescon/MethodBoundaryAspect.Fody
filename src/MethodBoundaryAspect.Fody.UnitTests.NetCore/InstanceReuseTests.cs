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
using static MethodBoundaryAspect.Fody.UnitTests.NetCore.WeaverTestHelper;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// An aspect which holds no state is created once instead of for every call, see
    /// <see cref="AspectInstanceReuseAnalyzer"/>. The analysis is conservative: every aspect which could be told
    /// from a new instance is created for every call.
    /// </summary>
    [Collection(RefStructWarningTests.TestAssemblyNetCoreWeaving)]
    public class InstanceReuseTests : IDisposable
    {
        private const string AspectsClass = "OnMethodBoundaryAspectCompile.Aspects";

        private readonly List<string> _weavedAssemblyPaths = new List<string>();

        [Theory]
        [InlineData(typeof(StatelessReuseAspect))]
        [InlineData(typeof(ValuesReuseAspect))]
        [InlineData(typeof(DerivedReuseAspect))]
        [InlineData(typeof(OptOutReuseAspect))]
        public void IfAspectHoldsNoState_ThenItCanBeReused(Type aspectType)
        {
            new AspectInstanceReuseAnalyzer(false, Enumerable.Empty<string>()).CanReuse(ResolveType(aspectType))
                .Should().BeTrue();
        }

        [Theory]
        [InlineData(typeof(CounterStateAspect), "writes a field")]
        [InlineData(typeof(MutableFieldAspect), "a field of a mutable type")]
        [InlineData(typeof(ArrayFieldAspect), "a field of a mutable type")]
        [InlineData(typeof(ConstructorWithEffectAspect), "the constructor does more than storing values")]
        [InlineData(typeof(SetterCallAspect), "calls a setter")]
        [InlineData(typeof(BaseSetterCallAspect), "calls a setter of the base class")]
        [InlineData(typeof(NonTrivialSetterAspect), "a setter does more than storing the value")]
        [InlineData(typeof(ThisLeakAspect), "hands out this")]
        [InlineData(typeof(LambdaThisAspect), "captures this")]
        [InlineData(typeof(FinalizerAspect), "has a finalizer")]
        public void IfAspectCouldHoldState_ThenItIsCreatedForEveryCall(Type aspectType, string reason)
        {
            new AspectInstanceReuseAnalyzer(false, Enumerable.Empty<string>()).CanReuse(ResolveType(aspectType))
                .Should().BeFalse(reason);
        }

        [Fact]
        public void IfAspectIsFromAReferenceAssembly_ThenItIsNotReused()
        {
            var aspectType = ResolveType(typeof(StatelessReuseAspect));
            var referenceAssemblyAttributeCtor = aspectType.Module.ImportReference(
                typeof(System.Runtime.CompilerServices.ReferenceAssemblyAttribute).GetConstructor(Type.EmptyTypes));
            aspectType.Module.Assembly.CustomAttributes.Add(new CustomAttribute(referenceAssemblyAttributeCtor));

            new AspectInstanceReuseAnalyzer(false, Enumerable.Empty<string>()).CanReuse(aspectType).Should().BeFalse();
        }

        [Fact]
        public void IfReuseIsDisabled_ThenNoAspectIsReused()
        {
            new AspectInstanceReuseAnalyzer(true, Enumerable.Empty<string>()).CanReuse(ResolveType(typeof(StatelessReuseAspect)))
                .Should().BeFalse();
        }

        [Fact]
        public void IfReuseIsDisabledForAnAspect_ThenOnlyThisAspectIsCreatedForEveryCall()
        {
            var analyzer = new AspectInstanceReuseAnalyzer(false, new[] { typeof(OptOutReuseAspect).FullName });

            analyzer.CanReuse(ResolveType(typeof(OptOutReuseAspect))).Should().BeFalse();
            analyzer.CanReuse(ResolveType(typeof(StatelessReuseAspect))).Should().BeTrue();
            analyzer.MatchedDisabledAspects.Should().Equal(typeof(OptOutReuseAspect).FullName);
        }

        [Theory]
        [InlineData(nameof(InstanceReuseMethods.Stateless), typeof(StatelessReuseAspect))]
        [InlineData(nameof(InstanceReuseMethods.WithValues), typeof(ValuesReuseAspect))]
        [InlineData(nameof(InstanceReuseMethods.Derived), typeof(DerivedReuseAspect))]
        public void IfAspectCanBeReused_ThenTheWeavedMethodGetsTheSharedInstance(string methodName, Type aspectType)
        {
            var module = Weave(config: null);

            var instructions = GetInstructions(module, methodName);

            CalledAspectGetters(instructions).Should().ContainSingle();
            CreatedTypes(instructions).Should().NotContain(aspectType.FullName);
        }

        [Fact]
        public void IfAspectCanBeReused_ThenTheSharedInstanceIsCreatedWithTheValuesOfTheAttribute()
        {
            var module = Weave(config: null);

            var getter = module.GetType(AspectsClass).Methods
                .Single(m => m.Name.StartsWith("GetAspect_") && m.Name.EndsWith(nameof(ValuesReuseAspect)));

            var instructions = getter.Body.Instructions;
            instructions.Should().Contain(i => i.OpCode == OpCodes.Ldstr && (string)i.Operand == "name");
            instructions.Should().Contain(i => i.OpCode == OpCodes.Ldstr && (string)i.Operand == "text");
            instructions.Should().Contain(i => i.OpCode == OpCodes.Newobj);
            instructions.Select(i => i.Operand).OfType<MethodReference>().Select(m => m.Name)
                .Should().Contain("set_Level");
        }

        [Theory]
        [InlineData(nameof(InstanceReuseMethods.Counter))]
        [InlineData(nameof(InstanceReuseMethods.MutableField))]
        [InlineData(nameof(InstanceReuseMethods.ConstructorWithEffect))]
        [InlineData(nameof(InstanceReuseMethods.ThisLeak))]
        [InlineData(nameof(InstanceReuseMethods.PrivateNestedAspect))] // another class can't create it
        public void IfAspectCannotBeReused_ThenItIsCreatedForEveryCall(string methodName)
        {
            var module = Weave(config: null);

            var instructions = GetInstructions(module, methodName);

            CalledAspectGetters(instructions).Should().BeEmpty();
            CreatedTypes(instructions).Should().Contain(t => t.EndsWith("Aspect") || t.EndsWith("Attribute"));
        }

        [Fact]
        public void IfAspectIsAppliedToAClass_ThenAllMethodsShareOneInstance()
        {
            var module = Weave(config: null);

            var first = CalledAspectGetters(GetInstructions(module, nameof(InstanceReuseClassAspect.First), typeof(InstanceReuseClassAspect)));
            var second = CalledAspectGetters(GetInstructions(module, nameof(InstanceReuseClassAspect.Second), typeof(InstanceReuseClassAspect)));

            first.Should().ContainSingle();
            second.Should().Equal(first);
        }

        [Fact]
        public void IfAsyncMethodIsWeaved_ThenTheSharedInstanceIsStoredInTheStateMachine()
        {
            var module = Weave(config: null);

            var instructions = GetInstructions(module, nameof(InstanceReuseMethods.Async));

            CalledAspectGetters(instructions).Should().ContainSingle();
            CreatedTypes(instructions).Should().NotContain(typeof(StatelessReuseAspect).FullName);
        }

        [Fact]
        public void IfAspectsAreReused_ThenTheCacheIsNotPartOfThePublicApi()
        {
            var module = Weave(config: null);

            var aspects = module.GetType(AspectsClass);

            aspects.IsNotPublic.Should().BeTrue();
            aspects.Methods.Should().NotContain(m => m.IsConstructor);
            aspects.Fields.Should().OnlyContain(f => f.IsPrivate && f.IsStatic && !f.IsInitOnly);
            aspects.Methods.Should().OnlyContain(m => m.IsAssembly && m.IsStatic && m.Body.InitLocals, "PEVerify wants initlocals");
        }

        [Fact]
        public void IfReuseIsDisabledInTheConfiguration_ThenEveryCallCreatesTheAspect()
        {
            var module = Weave(new XElement("MethodBoundaryAspect", new XAttribute("DisableAspectInstanceReuse", "true")));

            var instructions = GetInstructions(module, nameof(InstanceReuseMethods.Stateless));

            CalledAspectGetters(instructions).Should().BeEmpty();
            CreatedTypes(instructions).Should().Contain(typeof(StatelessReuseAspect).FullName);
            module.GetType(AspectsClass).Should().BeNull();
        }

        [Fact]
        public void IfReuseIsDisabledForAnAspectInTheConfiguration_ThenOnlyThisAspectIsCreatedForEveryCall()
        {
            var module = Weave(new XElement("MethodBoundaryAspect",
                new XElement("DisableAspectInstanceReuse", new XAttribute("Aspect", typeof(OptOutReuseAspect).FullName))));

            CalledAspectGetters(GetInstructions(module, nameof(InstanceReuseMethods.OptOut))).Should().BeEmpty();
            CalledAspectGetters(GetInstructions(module, nameof(InstanceReuseMethods.Stateless))).Should().ContainSingle();
        }

        [Fact]
        public void IfAnAspectOfTheConfigurationIsNotWeaved_ThenAWarningIsWritten()
        {
            var warnings = new List<string>();

            Weave(new XElement("MethodBoundaryAspect",
                new XElement("DisableAspectInstanceReuse", new XAttribute("Aspect", "Unknown.Aspect"))), warnings);

            warnings.Should().ContainSingle().Which.Should().Contain("Unknown.Aspect").And.Contain("DisableAspectInstanceReuse");
        }

        [Fact]
        public void IfConfigurationNamesNoAspect_ThenWeavingFails()
        {
            Action weave = () => Weave(new XElement("MethodBoundaryAspect", new XElement("DisableAspectInstanceReuse")));

            weave.Should().Throw<Exception>().WithMessage("*DisableAspectInstanceReuse*requires the full type name*");
        }

        private static IEnumerable<string> CreatedTypes(IEnumerable<Instruction> instructions) =>
            instructions.Where(i => i.OpCode == OpCodes.Newobj)
                .Select(i => ((MethodReference)i.Operand).DeclaringType.FullName);

        private static List<string> CalledAspectGetters(IEnumerable<Instruction> instructions) =>
            instructions.Where(i => i.OpCode == OpCodes.Call)
                .Select(i => (MethodReference)i.Operand)
                .Where(m => m.DeclaringType.FullName == AspectsClass)
                .Select(m => m.Name)
                .ToList();

        // the method in which the aspect is created: the weaved method, or the kick-off method of an async method
        private static IEnumerable<Instruction> GetInstructions(ModuleDefinition module, string methodName, Type type = null)
        {
            var method = module.GetType((type ?? typeof(InstanceReuseMethods)).FullName).Methods.Single(m => m.Name == methodName);
            return method.Body.Instructions;
        }

        private ModuleDefinition Weave(XElement config, List<string> warnings = null)
        {
var weaver = new ModuleWeaver { Config = config }.CaptureWarnings(warnings ?? new List<string>());
            weaver.AddClassFilter(typeof(InstanceReuseMethods).FullName);
            weaver.AddClassFilter(typeof(InstanceReuseClassAspect).FullName);

            var weavedAssemblyPath = weaver.WeaveToShadowFile(typeof(InstanceReuseMethods).Assembly.Location, ModuleHelper.AssemblyResolver);
            _weavedAssemblyPaths.Add(weavedAssemblyPath);

            return WeaverTestHelper.ReadWeavedModule(weavedAssemblyPath);
        }

        public void Dispose()
        {
            foreach (var path in _weavedAssemblyPaths)
                WeaverTestHelper.DeleteWeavedAssembly(path);
        }
    }
}
