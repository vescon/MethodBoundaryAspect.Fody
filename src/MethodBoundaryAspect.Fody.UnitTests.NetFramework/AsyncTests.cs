using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class AsyncTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Fact]
        public void IfAsyncStaticMethodIsWeavedForOnExit_ThenTaskIsPassedToAspectReturnValue()
        {
            // Arrange
            var testClassType = typeof(AsyncClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            object result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), "AttemptValue");

            // Assert
            result.Should().Be(4);
        }

        [Fact]
        public void IfAsyncStaticMethodIsWeavedForOnException_ThenExceptionThrownBetweenAwaitsIsHandled()
        {
            // Arrange
            var testClassType = typeof(AsyncClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            string result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), "AttemptThrow") as string;

            // Assert
            result.Should().Contain("OnException first");
            result.Should().Contain("OnException second");
        }

        [Fact]
        public void IfAsyncInstanceMethodIsWeavedForOnException_ThenExceptionThrownBetweenAwaitsIsHandled()
        {
            // Arrange
            var testClassType = typeof(AsyncClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            string result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), "AttemptInstance") as string;

            // Assert
            result.Should().Contain("OnException first");
            result.Should().Contain("OnException second");
        }

        [Fact]
        public void IfAsyncInstanceMethodIsWeavedForOnExit_ThenTaskIsPassedToAspectReturnValue()
        {
            // Arrange
            var testClassType = typeof(AsyncClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            object result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), "AttemptInstanceReturn");

            // Assert
            result.Should().Be(42);
        }

        [Fact]
        public void IfAsyncGenericMethodIsWeavedForOnException_ThenExceptionThrownBetweenAwaitsIsHandled()
        {
            // Arrange
            var testClassType = typeof(AsyncClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            string result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), "AttemptThrowT") as string;

            // Assert
            result.Should().Contain("OnException first");
            result.Should().Contain("OnException second");
        }

        [Fact]
        public void IfAsyncGenericMethodIsWeavedForOnExit_ThenTaskIsPassedToAspectReturnValue()
        {
            // Arrange
            var testClassType = typeof(AsyncClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            object result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), "AttemptIntReturn");

            // Assert
            result.Should().Be(10);
        }

        [Fact]
        public void IfAsyncMethodIsWeavedForOnExit_ThenOverwrittenReturnValueShouldBeHonored()
        {
            // Arrange
            var testClassType = typeof(AsyncOverwrite);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, "ReturnArg");
            object result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), "TestReturnArg", "Raw value");

            // Assert
            result.Should().Be("Everything is fine");
        }
    }
}
