using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using NUnit.Framework;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class SpecialAspectTests : MethodBoundaryAspectTestBase
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
        public void IfMethodWithAspectOnEntryOnlyIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithAspectOnEntryOnly";
            var testClassType = typeof(SpecialAspectMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            call.ShouldNotThrow();
        }

        [Test]
        public void IfMethodWithAspectOnExitOnlyIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithAspectOnExitOnly";
            var testClassType = typeof(SpecialAspectMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            call.ShouldNotThrow();
        }

        [Test]
        public void IfMethodWithAspectOnExceptionOnlyIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithAspectOnExceptionOnly";
            var testClassType = typeof(SpecialAspectMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            call.ShouldNotThrow();
        }
    }
}