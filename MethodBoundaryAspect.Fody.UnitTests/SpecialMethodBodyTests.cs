using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using NUnit.Framework;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class SpecialMethodBodyTests : MethodBoundaryAspectTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [Test]
        public void IfMethodBodyEndsWithBrtrueOpcode_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodBodyEndsWithBrtrueOpcode";
            var testClassType = typeof(SpecialMethodBodies);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            call.ShouldNotThrow();
        }

        [Test]
        public void IfMethodBodyWithBrtrueOpcodeEndsWithBrtrueOpcode_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodBodyWithBrtrueOpcodeEndsWithBrtrueOpcode";
            var testClassType = typeof(SpecialMethodBodies);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            call.ShouldNotThrow();
        }

        [Test]
        public void IfMethodBodyEndsWithBleOpcode_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodBodyEndsWithBleOpcode";
            var testClassType = typeof(SpecialMethodBodies);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            call.ShouldNotThrow();
        }
    }
}