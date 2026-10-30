using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    /// <summary>
    /// [DisableWeaving] on a class applies to its nested classes. The nested types have to be part of the weaving,
    /// otherwise they never are candidates and the assertion holds without the attribute.
    /// </summary>
    public class IgnoreWeavingForNestedClassAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Fact]
        public void IfTheOuterClassIsExcluded_ThenTheMethodsOfTheNestedClassAreNotWeaved()
        {
            // Arrange
            var testClassType = typeof(MultipleAspectsWithIgnoredNestedClass);
            AllowNothingWeaved = true;

            // Act
            WeaveAssemblyClassWithNestedTypesAndLoad(testClassType);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(0);
            Weaver.TotalWeavedTypes.Should().Be(0);
        }

        [Fact]
        public void IfTheOuterClassIsNotExcluded_ThenTheMethodsOfTheNestedClassAreWeaved()
        {
            // Arrange
            var testClassType = typeof(MultipleAspectsWithNestedClass);

            // Act
            WeaveAssemblyClassWithNestedTypesAndLoad(testClassType);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(2);
            Weaver.TotalWeavedTypes.Should().Be(1);
        }
    }
}
