using System;
using System.Linq;
using System.Xml.Linq;
using Fody;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Mono.Cecil;
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
