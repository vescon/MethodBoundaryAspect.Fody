using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class NonStandardPropertiesTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Fact]
        public void IfPropertyWithDoubleUnderscoreIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            var testClassType = typeof(NonStandardProperties);

            // Act
            Action call = () => WeaveAssemblyClassAndLoad(testClassType);

            // Assert
            call.Should().NotThrow();
        }
    }
}