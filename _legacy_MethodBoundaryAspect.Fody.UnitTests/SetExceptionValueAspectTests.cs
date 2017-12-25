using System;
using System.Reflection;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using NUnit.Framework;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class SetExceptionValueAspectTests : MethodBoundaryAspectTestBase
    {
        private static readonly Type TestClassType = typeof (SetExceptionValueAspectMethods);

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
        public void IfStaticMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "StaticMethodCall";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            Action call = () => AssemblyLoader.InvokeMethod(TestClassType.FullName, testMethodName);

            // Assert
            call.ShouldThrow<TargetInvocationException>()
                .WithInnerException<InvalidOperationException>()
                .WithInnerMessage(testMethodName);

            var result = AssemblyLoader.GetLastResult(TestClassType.FullName);
            result.Should().BeOfType<InvalidOperationException>();
        }

        [Test]
        public void IfInstanceMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "InstanceMethodCall";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            Action call = () => AssemblyLoader.InvokeMethod(TestClassType.FullName, testMethodName);

            // Assert
            call.ShouldThrow<TargetInvocationException>()
                .WithInnerException<InvalidOperationException>()
                .WithInnerMessage(testMethodName);

            var result = AssemblyLoader.GetLastResult(TestClassType.FullName);
            result.Should().BeOfType<InvalidOperationException>();
        }
    }
}