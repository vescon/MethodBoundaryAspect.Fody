using System;
using System.Reflection;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using NUnit.Framework;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class WeaveSpecialMethodTests : MethodBoundaryAspectTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [Test]
        public void IfExternMethodIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DoSomething";
            var testClassType = typeof(ExternMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            call.ShouldNotThrow();
        }

        [Test]
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

        [Test]
        public void IfAnonymousMethodIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "CallAnonymousMethod";
            var testClassType = typeof(AnonymousMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            var result = AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            result.Should().Be(testMethodName);
        }

        [Test]
        public void IfInstanceMethodCallWithTryCatchIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "InstanceMethodCallWithTryCatch";
            var testClassType = typeof(TryCatchMethods);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            call.ShouldThrow<TargetInvocationException>()
                .WithInnerException<InvalidOperationException>()
                .WithInnerMessage(testMethodName);
        }

        [Test]
        public void IfMethodWithTernaryOperatorAtTheEndAndTwoRetOpCodesIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithTernaryOperatorAtTheEndAndTwoRetOpCodes";
            var testClassType = typeof(SpecialOperatorMethods);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            object argument = new[] { "1", "2" };
            var result = AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName, argument);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            result.Should().Be(null);
        }

        [Test]
        public void IfMethodWithTryCatchAndUsingIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithTryCatchAndUsing";
            var testClassType = typeof(SpecialOperatorMethods);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            var result = AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            result.Should().Be(null);
        }

        [Test]
        public void IfMethodWithParameterWithMultipleReturnIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithParameterWithMultipleReturn";
            var testClassType = typeof(SpecialOperatorMethods);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            var result = AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName, 42);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            result.Should().Be(null);
        }

        [Test]
        public void IfMethodWithThrowIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "MethodWithThrowAndReturnValue";
            var testClassType = typeof(SpecialOperatorMethods);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, testMethodName);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            Weaver.TotalWeavedMethods.Should().Be(1);
            Weaver.TotalWeavedTypes.Should().Be(1);
            call.ShouldThrow<TargetInvocationException>()
                .WithInnerException<InvalidOperationException>()
                .WithInnerMessage("This is a test exception");
        }
    }
}