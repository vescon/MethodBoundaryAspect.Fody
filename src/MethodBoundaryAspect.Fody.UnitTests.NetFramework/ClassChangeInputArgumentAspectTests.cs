using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class ClassChangeInputArgumentAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private static readonly Type TestMethodsType = typeof (ChangeInputArgumentAspectMethods);
        
        [Fact]
        public void IfStaticMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "StaticMethodCallFirstArgument";
            WeaveAssemblyMethodAndLoad(TestMethodsType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.TypeInfo(), testMethodName, 99);

            // Assert
            result.Should().Be(42);
        }

        [Fact]
        public void IfInstanceMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "InstanceMethodCallSecondArgument";
            WeaveAssemblyMethodAndLoad(TestMethodsType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.TypeInfo(), testMethodName, 99, 999);

            // Assert
            result.Should().Be("42");
        }
        
        [Fact]
        public void IfInstanceMethodWithRefArgumentIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "InstanceMethodCallFirstArgumentArray";
            WeaveAssemblyMethodAndLoad(TestMethodsType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.TypeInfo(), testMethodName, new object());

            // Assert
            result.Should().BeOfType<object[]>();
        }

        [Fact]
        public void IfInstanceMethodWithAspectNotAllowedChangingInputArgumentsIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "InstanceMethodCallNotAllowedChangingInputArguments";
            WeaveAssemblyMethodAndLoad(TestMethodsType, testMethodName);

            // Act
            var guid = Guid.NewGuid();
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.TypeInfo(), testMethodName, guid);

            // Assert
            result.Should().Be(guid);
        }
    }
}