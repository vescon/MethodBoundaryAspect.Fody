using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    // https://github.com/vescon/MethodBoundaryAspect.Fody/issues/124
    public class SetConstructorArgumentFloatingPointAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private static readonly Type TestClassType = typeof(SetConstructorArgumentFloatingPointAspectMethods);

        private const string ExpectedResult =
            "Double: 4.9, Float: 1.5, Char: x, DoubleArray: [0.25, -3.5], Boxed: 2.75 (Double), NamedDouble: 6.125";

        [Fact]
        public void IfStaticMethodWithFloatingPointArgumentsIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "StaticMethodCall";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be(ExpectedResult);
        }

        [Fact]
        public void IfInstanceMethodWithFloatingPointArgumentsIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "InstanceMethodCall";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be(ExpectedResult);
        }
    }
}
