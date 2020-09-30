using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class SpecialAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Fact]
        public void IfMethodWithAspectOnEntryOnlyIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithAspectOnEntryOnly";
            var testClassType = typeof(SpecialAspectMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName);

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
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName);

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
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName);

            // Assert
            call.Should().NotThrow();
        }
    }
}