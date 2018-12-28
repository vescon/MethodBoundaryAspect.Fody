using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class IgnoreWeavingForMethodsAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Fact]
        public void IfStaticMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            var testClassType = typeof(MultipleAspectsWithIgnoredMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
        }
    }
}