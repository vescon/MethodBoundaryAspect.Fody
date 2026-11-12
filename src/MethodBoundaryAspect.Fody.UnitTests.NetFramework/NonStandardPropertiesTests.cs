using System;
using AwesomeAssertions;
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
            Action call = () => WeaveAssemblyPropertyAndLoad(testClassType, nameof(NonStandardProperties.Property_WithDouble_Underscore));

            // Assert
            call.Should().NotThrow();
            // the accessors are woven (without an aspect the assembly is valid as well)
            Weaver.TotalWeavedProperties.Should().Be(2);
        }
    }
}