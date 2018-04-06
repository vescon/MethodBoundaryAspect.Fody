using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class SpecialAspectTests : MethodBoundaryAspectTestBase
    {
        [Fact]
        public void IfMethodWithAspectOnEntryOnlyIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithAspectOnEntryOnly";
            var testClassType = typeof(SpecialAspectMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            call.Should().NotThrow();
        }

        [Fact]
        public void IfMethodWithAspectOnExitOnlyIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithAspectOnExitOnly";
            var testClassType = typeof(SpecialAspectMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            call.Should().NotThrow();
        }

        [Fact]
        public void IfMethodWithAspectOnExceptionOnlyIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithAspectOnExceptionOnly";
            var testClassType = typeof(SpecialAspectMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            call.Should().NotThrow();
        }
    }
}