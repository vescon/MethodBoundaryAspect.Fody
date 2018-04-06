using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class NonStandardPropertiesTests : MethodBoundaryAspectTestBase
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