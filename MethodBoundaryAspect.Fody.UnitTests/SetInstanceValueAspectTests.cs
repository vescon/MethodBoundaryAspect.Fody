using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using NUnit.Framework;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class SetInstanceValueAspectTests : MethodBoundaryAspectTestBase
    {
        private static readonly Type TestClassType = typeof (SetInstanceValueAspectMethods);

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
        public void IfStaticMethodWithValueTypeIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "StaticMethodCall";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.FullName, testMethodName);

            // Assert
            result.Should().Be("instance was null");
        }

        [Test]
        public void IfInstanceMethodWithValueTypeIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalled()
        {
            // Arrange
            const string testMethodName = "InstanceMethodCall";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.FullName, testMethodName);

            // Assert
            result.Should().Be("SetInstanceValueAspectMethods->ToString()");
        }
    }
}