using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class SetMethodNameAspectTests : MethodBoundaryAspectTestBase
    {
        private static readonly Type TestMethodsType = typeof (SetMethodNameAspectMethods);

        private static readonly Type TestClassType = typeof (ClassSetMethodNameAspect);
        private static readonly Type TestClassResultType = typeof (ClassSetMethodNameAspectResult);
        
        [Fact]
        public void IfStaticMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "StaticMethodCall";
            WeaveAssemblyMethodAndLoad(TestMethodsType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.FullName, testMethodName);

            // Assert
            result.Should().Be("StaticMethodCall");
        }

        [Fact]
        public void IfInstanceMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "InstanceMethodCall";
            WeaveAssemblyMethodAndLoad(TestMethodsType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.FullName, testMethodName);

            // Assert
            result.Should().Be("InstanceMethodCall");
        }

        [Fact]
        public void IfClassStaticMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "ClassStaticMethodCall";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethodWithResultClass(
                TestClassResultType.FullName,
                TestClassType.FullName,
                testMethodName);

            // Assert
            result.Should().Be(testMethodName);
        }

        [Fact]
        public void IfClassInstanceMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "ClassInstanceMethodCall";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethodWithResultClass(
                TestClassResultType.FullName,
                TestClassType.FullName,
                testMethodName);

            // Assert
            result.Should().Be(testMethodName);
        }
    }
}