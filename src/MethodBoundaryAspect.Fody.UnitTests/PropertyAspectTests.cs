using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class PropertyAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private static readonly Type TestClassType = typeof(PropertyAspectMethods);
        
        [Fact]
        public void IfStaticMethodWithValueTypeIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "StaticMethodCall";
            WeaveAssemblyPropertyAndLoad(TestClassType, "StaticProperty");

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("[set_StaticProperty][get_StaticProperty]");
        }

        [Fact]
        public void IfInstanceMethodWithValueTypeIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "InstanceMethodCall";
            WeaveAssemblyPropertyAndLoad(TestClassType, "InstanceProperty");

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("[set_InstanceProperty][get_InstanceProperty]");
        }
    }
}