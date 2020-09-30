using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class IgnoreWeavingForNestedClassAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Fact]
        public void IfStaticMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            var testClassType = typeof(MultipleAspectsWithIgnoredNestedClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(0);
            Weaver.TotalWeavedTypes.Should().Be(0);
        }
    }
}