using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class SetMethodExecutionTagValueOnExceptionAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private static readonly Type TestClassType = typeof(SetMethodExecutionTagValueOnExceptionAspectMethods);
        
        [Fact]
        public void IfStaticMethodWithValueTypeIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "StaticMethodCall";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethodSwallowException(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("MethodExecutionTag1");
        }

        [Fact]
        public void IfInstanceMethodWithValueTypeIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "InstanceMethodCall";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethodSwallowException(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("MethodExecutionTag1");
        }
    }
}