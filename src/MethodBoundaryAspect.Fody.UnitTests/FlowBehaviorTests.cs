using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using System;
using System.Reflection;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class FlowBehaviorTests : MethodBoundaryAspectTestBase
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
    }
}
