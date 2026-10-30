using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class IgnoreWeavingForClassAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Fact]
        public void IfTheClassIsExcluded_ThenNoMethodIsWeaved()
        {
            // Arrange
            AllowNothingWeaved = true;
            var testClassType = typeof(MultipleAspectsWithIgnoredClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(0);
            Weaver.TotalWeavedTypes.Should().Be(0);
        }
    }
}