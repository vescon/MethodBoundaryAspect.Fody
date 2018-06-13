using System;
using System.Reflection;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class SpecialMethodTests : MethodBoundaryAspectTestBase
    {
        [Fact]
        public void IfExternMethodIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DoSomething";
            var testClassType = typeof(ExternMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName);

            // Assert
            call.Should().NotThrow();
        }

        [Fact]
        public void IfInterfaceMethodIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            var testClassType = typeof(IInterfaceMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(0);
            Weaver.TotalWeavedTypes.Should().Be(0);
        }

        [Fact]
        public void IfAnonymousMethodIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "CallAnonymousMethod";
            var testClassType = typeof(AnonymousMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            result.Should().Be(testMethodName);
        }

        [Fact]
        public void IfInstanceMethodCallWithTryCatchIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "InstanceMethodCallWithTryCatch";
            var testClassType = typeof(TryCatchMethods);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            call.Should().Throw<TargetInvocationException>()
                .WithInnerException<InvalidOperationException>()
                .WithMessage(testMethodName);
        }

        [Fact]
        public void IfMethodWithTernaryOperatorAtTheEndAndTwoRetOpCodesIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithTernaryOperatorAtTheEndAndTwoRetOpCodes";
            var testClassType = typeof(SpecialOperatorMethods);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            object argument = new[] { "1", "2" };
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName, argument);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            result.Should().Be(null);
        }

        [Fact]
        public void IfMethodWithTryCatchAndUsingIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithTryCatchAndUsing";
            var testClassType = typeof(SpecialOperatorMethods);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            result.Should().Be(null);
        }

        [Fact]
        public void IfMethodWithParameterWithMultipleReturnIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithParameterWithMultipleReturn";
            var testClassType = typeof(SpecialOperatorMethods);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName, 42);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            result.Should().Be("");
        }

        [Fact]
        public void IfMethodWithThrowIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithThrowAndReturnValue";
            var testClassType = typeof(SpecialOperatorMethods);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            call.Should().Throw<TargetInvocationException>()
                .WithInnerException<InvalidOperationException>()
                .WithMessage("This is a test exception");
        }

        [Fact]
        public void IfGenericClassIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DoIt";
            var testClassType = typeof(GenericClass<>);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
        }

        [Fact]
        public void IfClassWithGenericMethodIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DoIt";
            var testClassType = typeof(ClassWithGenericMethod);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
        }

        [Fact]
        public void IfClassWithGenericMethodWithImplementedConstraintIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DoItWithImplementedConstraint";
            var testClassType = typeof(ClassWithGenericMethodWithConstraint);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            var argument = AssemblyLoader.CreateInstance(nameof(TestClass));
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName, argument);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            result.Should().Be("Test succeeded");
        }

        [Fact]
        public void IfClassWithGenericMethodWithImplementedConstraintIsWeaved_ThenTheAssemblyShouldBeValid_InvalidClass()
        {
            // Arrange
            const string testMethodName = "DoItWithImplementedConstraint";
            var testClassType = typeof(ClassWithGenericMethodWithConstraint);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName, new InvalidTestClass("test"));

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            call.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void IfClassWithGenericMethodWithClassConstraintIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DoItWithClassConstraint";
            var testClassType = typeof(ClassWithGenericMethodWithConstraint);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            var argument = AssemblyLoader.CreateInstance(nameof(TestClass));
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName, argument);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            call.Should().NotThrow<Exception>();
        }

        [Fact]
        public void IfClassWithGenericMethodWithStructConstraintIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DoItWithStructConstraint";
            var testClassType = typeof(ClassWithGenericMethodWithConstraint);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            var argument = AssemblyLoader.CreateInstance(nameof(TestStruct));
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName, argument);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            call.Should().NotThrow<Exception>();
        }

        [Fact]
        public void IfClassWithGenericMethodWithNewConstraintIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DoItWithNewConstraint";
            var testClassType = typeof(ClassWithGenericMethodWithConstraint);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            var argument = AssemblyLoader.CreateInstance(nameof(TestClass));
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName, argument);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            call.Should().NotThrow<Exception>();
        }

        [Fact]
        public void IfClassWithGenericMethodWithNewConstraintIsWeaved_ThenTheAssemblyShouldBeValid_WithInvalidTestClass()
        {
            // Arrange
            const string testMethodName = "DoItWithNewConstraint";
            var testClassType = typeof(ClassWithGenericMethodWithConstraint);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName, new InvalidTestClass("Test"));

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            call.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void IfGenericClassWithGenericMethodIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DoIt";
            var testClassType = typeof(GenericClassWithGenericMethod<>);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
        }

        [Fact]
        public void IfGenericClassWithGenericMethodWithReturnIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DoItWithReturn";
            var testClassType = typeof(GenericClassWithGenericMethod<>);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
        }

        [Fact(Skip = "Issue #33")]
        public void IfAGenericClassWithClassConstraintWithAMethodIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DoIt";
            var testClassType = typeof(GenericClassWithConstraintsAndBase<>);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfoWithGenericParameters(typeof(Entity)), testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            call.Should().NotThrow<Exception>();
        }
    }
}