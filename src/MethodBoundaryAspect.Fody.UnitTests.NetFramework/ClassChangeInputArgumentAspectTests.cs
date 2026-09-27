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
            result.Should().BeOfType<int[]>();
            var arguments = AssemblyLoader.Arguments;
            arguments[0].Should().BeOfType<int[]>();

            var modifiedArgument = arguments[0];
            modifiedArgument.Should().BeEquivalentTo(result); // testing for referential equality is not possible here because of different app domains
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

        [Fact]
        public void IfInstanceMethodWithOutValueTypeArgumentsIsCalled_ThenTheOutArgumentsShouldBeSet()
        {
            // Arrange (https://github.com/vescon/MethodBoundaryAspect.Fody/issues/127)
            const string testMethodName = "InstanceMethodWithOutGuidArguments";
            WeaveAssemblyMethodAndLoad(TestMethodsType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.TypeInfo(), testMethodName, Guid.Empty, Guid.Empty);

            // Assert
            var expectedGuids = result.Should().BeOfType<Guid[]>().Subject;
            expectedGuids.Should().NotContain(Guid.Empty);

            var arguments = AssemblyLoader.Arguments;
            arguments.Should().Equal(expectedGuids[0], expectedGuids[1]);
        }

        [Fact]
        public void IfInstanceMethodWithOutPrimitiveArgumentIsCalled_ThenTheOutArgumentShouldBeSet()
        {
            // Arrange
            const string testMethodName = "InstanceMethodWithOutIntArgument";
            WeaveAssemblyMethodAndLoad(TestMethodsType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.TypeInfo(), testMethodName, 0);

            // Assert
            result.Should().Be(42);
            AssemblyLoader.Arguments[0].Should().Be(42);
        }

        [Fact]
        public void IfInstanceMethodWithRefAndOutArgumentsIsCalled_ThenOnExitShouldSeeTheAssignedValues()
        {
            // Arrange
            const string testMethodName = "InstanceMethodWithRefAndOutArgumentsCapturedOnExit";
            WeaveAssemblyMethodAndLoad(TestMethodsType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.TypeInfo(), testMethodName, "value", Guid.Empty, 0, null);

            // Assert
            var expectedGuid = new Guid("5b9a1f3e-4c2d-4e8f-9a7b-1c2d3e4f5a6b");
            result.Should().BeOfType<object[]>()
                .Which.Should().Equal("value", expectedGuid, 42, "value42");
            AssemblyLoader.Arguments.Should().Equal("value", expectedGuid, 42, "value42");
        }

        [Fact]
        public void IfInstanceMethodWithOutArgumentReturnsEarlyInOnEntry_ThenTheOutArgumentShouldBeSetFromAspect()
        {
            // Arrange
            const string testMethodName = "InstanceMethodWithOutIntArgumentAndEarlyReturn";
            WeaveAssemblyMethodAndLoad(TestMethodsType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestMethodsType.TypeInfo(), testMethodName, 0);

            // Assert
            result.Should().BeNull();
            AssemblyLoader.Arguments[0].Should().Be(42);
        }
    }
}