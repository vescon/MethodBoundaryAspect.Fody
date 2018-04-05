using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class MultipleAspectsTests : MethodBoundaryAspectTestBase
    {
        private static readonly Type TestClassType = typeof(MultipleAspectsMethods);
        
        [Fact]
        public void IfStaticMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "StaticMethodCall";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.FullName, testMethodName);

            // Assert
            result.Should().Be("[FirstAspect_OnEntry][SecondAspect_OnEntry][SecondAspect_OnExit][FirstAspect_OnExit]");
        }

        [Fact]
        public void IfInstanceMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "InstanceMethodCall";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.FullName, testMethodName);

            // Assert
            RunIlSpy();
            result.Should().Be("[FirstAspect_OnEntry][SecondAspect_OnEntry][SecondAspect_OnExit][FirstAspect_OnExit]");
        }
    }
}