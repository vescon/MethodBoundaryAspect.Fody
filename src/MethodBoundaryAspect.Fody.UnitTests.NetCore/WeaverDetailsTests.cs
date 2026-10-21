using System;
using System.Linq;
using System.Xml.Linq;
using Fody;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// Details of the weaver found in a review: statistics, configuration, filters and cloned generic parameters.
    /// The module is woven in memory.
    /// </summary>
    public class WeaverDetailsTests
    {
        // GenericParameterAttributes.AllowByRefLike ("allows ref struct"), not known by Mono.Cecil 0.11
        private const GenericParameterAttributes AllowByRefLike = (GenericParameterAttributes)0x0020;

        [Fact]
        public void IfTheLastMethodOfATypeIsNotWoven_ThenTheTypeIsCountedAsWoven()
        {
            var weaver = CreateWeaver(typeof(LastMethodNotWovenMethods));

            weaver.Execute();

            weaver.TotalWeavedMethods.Should().Be(1);
            weaver.TotalWeavedTypes.Should().Be(1);
        }

        [Fact]
        public void IfAGenericMethodIsWoven_ThenTheExecutorKeepsTheFlagsOfTheGenericParameters()
        {
            var weaver = CreateWeaver(typeof(GenericParameterFlagsMethods));
            var type = weaver.ModuleDefinition.GetType(typeof(GenericParameterFlagsMethods).FullName);
            // "where T : allows ref struct" can't be written in C# 8, it has no type constraint
            type.Methods.Single(m => m.Name == nameof(GenericParameterFlagsMethods.Generic)).GenericParameters[0].Attributes |= AllowByRefLike;

            weaver.Execute();

            var executor = type.Methods.Single(m => m.Name == "$_executor_" + nameof(GenericParameterFlagsMethods.Generic));
            executor.GenericParameters[0].Attributes.Should().Be(AllowByRefLike);
        }

        [Fact]
        public void IfAPropertyFilterIsSet_ThenOnlyTheseAccessorsAreWoven()
        {
            var weaver = CreateWeaver(typeof(PropertyFilterMethods));
            weaver.AddPropertyFilter(typeof(PropertyFilterMethods).FullName + ".get_Value");

            weaver.Execute();

            weaver.TotalWeavedProperties.Should().Be(1);
            var type = weaver.ModuleDefinition.GetType(typeof(PropertyFilterMethods).FullName);
            type.Methods.Select(m => m.Name).Should().Contain("$_executor_get_Value").And.NotContain("$_executor_set_Value");
        }

        [Fact]
        public void IfARefArrayOfEnumsIsWrittenBack_ThenTheReferenceIsStored()
        {
            var weaver = CreateWeaver(typeof(RefArrayMethods));

            weaver.Execute();

            // Resolve() of an enum array returns the enum, stind.i4 only wrote 4 bytes of the reference
            var method = weaver.ModuleDefinition.GetType(typeof(RefArrayMethods).FullName).Methods
                .Single(m => m.Name == nameof(RefArrayMethods.SetColors));
            var stores = method.Body.Instructions
                .Where(i => i.OpCode.Name.StartsWith("stind") || i.OpCode == OpCodes.Stobj)
                .Select(i => i.OpCode)
                .ToList();
            stores.Should().NotBeEmpty().And.OnlyContain(opCode => opCode == OpCodes.Stind_Ref);
            method.Body.Instructions.Should().NotContain(i => i.OpCode == OpCodes.Box);
        }

        [Fact]
        public void IfAFilterIsNoValidRegularExpression_ThenWeavingFailsWithTheAspectAndFilter()
        {
            var weaver = CreateWeaver(typeof(InvalidFilterMethods));
            var aspect = weaver.ModuleDefinition.GetType(typeof(InvalidFilterMethods).FullName).Methods
                .Single(m => m.Name == nameof(InvalidFilterMethods.Filtered)).CustomAttributes.Single();
            var filter = aspect.Properties.Single();
            aspect.Properties[0] = new CustomAttributeNamedArgument(filter.Name, new CustomAttributeArgument(filter.Argument.Type, "(unclosed"));

            Action weave = () => weaver.Execute();

            weave.Should().Throw<WeavingException>()
                .WithMessage("Aspect 'MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects.OnlyOnEntryAspect' has an invalid regular expression in MethodNameFilter: *");
        }

        [Fact]
        public void IfTheConfigurationOfTheAspectsIsInvalid_ThenWeavingFailsWithTheMethod()
        {
            var weaver = CreateWeaver(typeof(MultipleAspectsMethods));
            var module = weaver.ModuleDefinition;
            var method = module.GetType(typeof(MultipleAspectsMethods).FullName).Methods
                .Single(m => m.Name == nameof(MultipleAspectsMethods.Add));
            // an order index for one of two aspects: all aspects of a method need one
            var aspectType = method.CustomAttributes[0].AttributeType;
            var runtimeModule = aspectType.Resolve().BaseType.Resolve().Module;
            var orderIndexType = runtimeModule.GetType("MethodBoundaryAspect.Fody.Attributes.AspectOrderIndexAttribute");
            var orderIndex = new CustomAttribute(module.ImportReference(orderIndexType.Methods.Single(m => m.IsConstructor)));
            orderIndex.ConstructorArguments.Add(new CustomAttributeArgument(module.ImportReference(typeof(Type)), aspectType));
            orderIndex.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.Int32, 1));
            method.CustomAttributes.Add(orderIndex);

            Action weave = () => weaver.Execute();

            weave.Should().Throw<WeavingException>()
                .Which.Message.Should()
                .Contain("MultipleAspectsMethods::Add")
                .And.Contain("No order index provided for aspect")
                .And.Contain("OnlyOnExitAspect");
        }

        [Fact]
        public void IfAnAssemblyIsWeavedToAShadowFileTwiceInARow_ThenTheFilesHaveDifferentNames()
        {
            // named by Environment.TickCount, whose resolution is about 16 ms: a second weave within it overwrote the first
            var assemblyPath = typeof(VarArgMethods).Assembly.Location;
            var weaver = new ModuleWeaver();
            weaver.AddClassFilter(typeof(VarArgMethods).FullName);

            var first = weaver.WeaveToShadowFile(assemblyPath, ModuleHelper.AssemblyResolver);
            var second = weaver.WeaveToShadowFile(assemblyPath, ModuleHelper.AssemblyResolver);

            try
            {
                first.Should().NotBe(second);
                System.IO.File.Exists(first).Should().BeTrue();
                System.IO.File.Exists(second).Should().BeTrue();
                System.IO.Path.GetFileName(first).Should().MatchRegex("^_[0-9a-f]{12}_.+_Weaved_\\.dll$", "the prefix is a random id");
            }
            finally
            {
                foreach (var path in new[] { first, second })
                {
                    System.IO.File.Delete(path);
                    System.IO.File.Delete(System.IO.Path.ChangeExtension(path, "pdb"));
                }
            }
        }

        [Fact]
        public void IfOneAspectIsAppliedToManyMethods_ThenAllOfThemAreWoven()
        {
            var weaver = CreateWeaver(typeof(ManyMethodsOfOneAspect));

            weaver.Execute();

            weaver.TotalWeavedMethods.Should().Be(4);
            weaver.ModuleDefinition.GetType(typeof(ManyMethodsOfOneAspect).FullName).Methods
                .Count(m => m.Name.StartsWith("$_executor_")).Should().Be(4);
        }

        [Fact]
        public void IfATypeIsNamedVoid_ThenItIsNotVoid()
        {
            var module = ModuleDefinition.CreateModule("Void", ModuleKind.Dll);
            var ownVoid = new TypeDefinition("MyNamespace", "Void", TypeAttributes.Class | TypeAttributes.Public, module.TypeSystem.Object);
            module.Types.Add(ownVoid);

            InstructionBlockCreator.IsVoid(module.TypeSystem.Void).Should().BeTrue();
            // RequiredModifierType(modifier, type)
            InstructionBlockCreator.IsVoid(new RequiredModifierType(module.TypeSystem.Object, module.TypeSystem.Void)).Should().BeTrue("void modreq(IsExternalInit) of an init accessor");
            InstructionBlockCreator.IsVoid(new RequiredModifierType(module.TypeSystem.Void, module.TypeSystem.Object)).Should().BeFalse();
            InstructionBlockCreator.IsVoid(ownVoid).Should().BeFalse();
            InstructionBlockCreator.IsVoid(module.TypeSystem.Int32).Should().BeFalse();
        }

        [Fact]
        public void IfAnArgumentAfterThe255thIsLoaded_ThenItIsLoadedByItsParameter()
        {
            var module = ModuleDefinition.CreateModule("Arguments", ModuleKind.Dll);
            var method = new MethodDefinition("Many", MethodAttributes.Public, module.TypeSystem.Void);
            for (var i = 0; i < 300; i++)
                method.Parameters.Add(new ParameterDefinition("p" + i, ParameterAttributes.None, module.TypeSystem.Int32));
            var parameter = method.Parameters[299];

            // instance method: argument 300, ldarg.s with (byte)300 loaded argument 44
            var load = new ArgumentLoadable(300, parameter, method.Body.GetILProcessor()).Load(false, false);

            load.Instructions.Should().ContainSingle().Which.Operand.Should().BeSameAs(parameter);
        }

        [Fact]
        public void IfAMethodHasMultipleAspects_ThenEachLocalVariableIsDeclaredOnce()
        {
            var weaver = CreateWeaver(typeof(MultipleAspectsMethods));

            weaver.Execute();

            // the variable for the MethodExecutionTag of each aspect was added twice
            var method = weaver.ModuleDefinition.GetType(typeof(MultipleAspectsMethods).FullName).Methods
                .Single(m => m.Name == nameof(MultipleAspectsMethods.Add));
            method.Body.Variables.Should().OnlyHaveUniqueItems();
        }

        [Theory]
        [InlineData("SuppressRefStructWarnings")]
        [InlineData("DisableExecutionArgsOptimization")]
        public void IfABooleanConfigValueIsInvalid_ThenWeavingFails(string attributeName)
        {
            var weaver = CreateWeaver(typeof(LastMethodNotWovenMethods));
            weaver.Config = new XElement("MethodBoundaryAspect", new XAttribute(attributeName, "yes"));

            Action weave = () => weaver.Execute();

            weave.Should().Throw<WeavingException>()
                .WithMessage($"Invalid value 'yes' for {attributeName} in FodyWeavers.xml, expected 'true' or 'false'.");
        }

        private static ModuleWeaver CreateWeaver(Type type)
        {
            var weaver = new ModuleWeaver
            {
                ModuleDefinition = WeaverTestHelper.ReadModule(type)
            };
            weaver.AddClassFilter(type.FullName);
            return weaver;
        }
    }
}
