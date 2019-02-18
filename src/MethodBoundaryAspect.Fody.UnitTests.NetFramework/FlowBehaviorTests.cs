using System;
using System.Reflection;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class FlowBehaviorTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private static readonly Type TestClassType = typeof(FlowBehaviorClass);

        [Fact]
        public void IfFlowBehaviorIsDefault_ThenExceptionIsRethrown()
        {
            // Arrange
            const string testMethodName = "Default";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            Action a = () => AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            a.Should().ThrowExactly<TargetInvocationException>()
             .Subject.Should().ContainSingle()
             .Which.InnerException.Message
             .Should().Be("An exception");
        }

        [Fact]
        public void IfFlowBehaviorIsRethrow_ThenExceptionIsRethrown()
        {
            // Arrange
            const string testMethodName = "Rethrow";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            Action a = () => AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            a.Should().ThrowExactly<TargetInvocationException>()
             .Subject.Should().ContainSingle()
             .Which.InnerException.Message
             .Should().Be("An exception");
        }

        [Fact]
        public void IfFlowBehaviorIsContinue_ThenExceptionIsSuppressed()
        {
            // Arrange
            const string testMethodName = "Suppress";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            Action a = () => AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            a.Should().NotThrow();
        }

        [Fact]
        public void IfFlowBehaviorIsContinue_ThenReturnValueIsReadFromArgs()
        {
            // Arrange
            const string testMethodName = "SuppressAndReturnValueType";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            object result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be(42);
        }

        [Fact]
        public void IfFlowBehaviorIsContinue_ThenReturnValueStringIsReadFromArgs()
        {
            // Arrange
            const string testMethodName = "SuppressAndReturnString";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            object result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("Changed");
        }

        [Fact]
        public void IfFlowBehaviorIsReturnAfterOnEntry_ThenOriginalMethodIsNotExecuted()
        {
            // Arrange
            const string testMethodName = "TryReturn";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            object result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be(3);
        }

        [Fact]
        public void IfFlowBehaviorIsReturnAfterOnEntry_ThenOriginalVoidMethodIsNotExecuted()
        {
            // Arrange
            const string testMethodName = "TryReturnVoid";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            Action a = () => AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            a.Should().NotThrow();
        }

        [Fact]
        public void IfFlowBehaviorIsReturnAfterOnEntry_ThenRemainingAspectsAreNotExecuted()
        {
            // Arrange
            const string testMethodName = "AbortAspects";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            object result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be(6);
        }

        [Fact]
        public void IfFlowBehaviorIsReturn_ThenExceptionIsSuppressed()
        {
            // Arrange
            const string testMethodName = "SuppressReturn";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            Action a = () => AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            a.Should().NotThrow();
        }

        [Fact]
        public void IfFlowBehaviorIsReturnAfterOnEntry_ThenPreviousAspectsOnExitAreStillCalled()
        {
            // Arrange
            const string testMethodName = "ExitTest";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            object result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be(6);
        }

        [Fact]
        public void IfFlowBehaviorIsReturnAfterOnException_ThenSubsequentAspectsOnExitAreCalledInsteadOfOnException()
        {
            // Arrange
            const string testMethodName = "ExitAfterException";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            object result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be(6);
        }

        [Fact]
        public void IfFlowBehaviorIsReturnAfterAsyncOnException_ThenNoOtherAspectsOnExceptionAreCalled()
        {
            // Arrange
            const string testMethodName = "ExitAfterAsyncException";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            object result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), $"Test{testMethodName}");

            // Assert
            result.Should().Be(6);
        }
    }
}
