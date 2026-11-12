using System;
using AwesomeAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// PEVerify does not check the woven assemblies of .NET Core and .NET Standard (it can't verify them) and the code is
    /// not run here, so these tests only show that weaving something does not throw. The woven code of these
    /// targets runs in the RuntimeTests.
    /// </summary>
    [Collection(RefStructWarningTests.TestAssemblyNetCoreWeaving)]
    public class SpecialAspectWeavingTests : MethodBoundaryAspectTestBase
    {
        [Fact]
        public void IfNetCoreClassIsWeaved_ThenWeavingShouldNotThrow()
        {
            // Arrange
            var testClassType = typeof(SpecialAspectMethods);

            // Act
            Action call = () => WeaveAssemblyClass(testClassType);

            // Assert
            call.Should().NotThrow();
        }

        [Fact]
        public void IfNetStandardClassIsWeaved_ThenWeavingShouldNotThrow()
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