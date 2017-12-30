using System;
using System.Reflection;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class SpecialMethodBodyTests : MethodBoundaryAspectTestBase
    {
        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
        public void IfMethodWhichEndsWithThrowAndHasMultipleReturns_2_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWhichEndsWithThrowAndHasMultipleReturns";
            var testClassType = typeof(SpecialMethodBodies);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            var result = AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName, 2);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            result.Should().Be("2");
        }

        [Fact]
        public void IfMethodWhichEndsWithThrowAndHasMultipleReturns_3_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWhichEndsWithThrowAndHasMultipleReturns";
            var testClassType = typeof(SpecialMethodBodies);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName, 3);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            call.ShouldThrow<TargetInvocationException>()
                .WithInnerException<InvalidOperationException>()
                .WithInnerMessage("This exception is expected");
        }

        [Fact]
        public void IfStrangeMethodForIssue9_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "StrangeMethodForIssue9";
            var testClassType = typeof(SpecialMethodBodies);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName, true);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
        }

        [Fact]
        public void IfStrangeMethodForIssue10_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "StrangeMethodForIssue10";
            var testClassType = typeof(SpecialMethodBodies);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName, true);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
        }
    }
}