using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class DuplicateEntryMethodsAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private static readonly Type TestMethodsType = typeof(DuplicateEntryMethodsAspectMethods);

        [Fact]
        public void IfStaticMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "StaticMethodCall";
            WeaveAssemblyMethodAndLoad(TestMethodsType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("-OnEntry()-OnExit()");
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
            result.Should().Be("-OnEntry()-OnExit()");
        }
    }
}