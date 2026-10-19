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
                ModuleDefinition = ModuleDefinition.ReadModule(type.Assembly.Location,
                    new ReaderParameters { AssemblyResolver = ModuleHelper.AssemblyResolver })
            };
            weaver.AddClassFilter(type.FullName);
            return weaver;
        }
    }
}
