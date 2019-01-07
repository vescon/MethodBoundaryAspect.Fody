using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    public class SpecialAspectTests : MethodBoundaryAspectTestBase
    {
        [Fact]
        public void IfNetCoreClassIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            var testClassType = typeof(SpecialAspectMethods);

            // Act
            Action call = () => WeaveAssemblyClass(testClassType);

            // Assert
            call.Should().NotThrow();
        }

        [Fact]
        public void IfNetStandardClassIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            var testClassType = typeof(TestAssembly.NetStandard.SpecialAspectMethods);

            // Act
            Action call = () => WeaveAssemblyClass(testClassType);

            // Assert
            call.Should().NotThrow();
        }
    }
}