using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class SetMethodNameAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private static readonly Type TestMethodsType = typeof (SetMethodNameAspectMethods);
        private static readonly Type TestClassType = typeof (ClassSetMethodNameAspect);
        
        [Fact]
        public void IfStaticMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "StaticMethodCall";
            WeaveAssemblyMethodAndLoad(TestMethodsType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.TypeInfo(), testMethodName);

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
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.TypeInfo(), testMethodName);

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
            var result = AssemblyLoader.InvokeMethod(
                TestClassType.TypeInfo(),
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
            var result = AssemblyLoader.InvokeMethod(
                TestClassType.TypeInfo(),
                testMethodName);

            // Assert
            result.Should().Be(testMethodName);
        }
    }
}