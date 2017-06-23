using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.Ordering;
using MethodBoundaryAspect.Fody.UnitTests.Unified;
using NUnit.Framework;

namespace MethodBoundaryAspect.Fody.UnitTests.MultipleAspects.FailTests
{
    [TestFixture]
    public class Tests : UnifiedWeaverTestBase
    {
        private readonly Type _testType = typeof (TestFailMethods);

        [SetUp]
        public override void Setup()
        {
            base.Setup();
        }

        [Test]
        public void IfVoidEmptyMethodMethodIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(_testType.FullName + ".VoidEmptyMethod");

            // Act
            Action call = () => weaver.Weave(WeaveDll);

            // Arrange
            call.ShouldThrow<InvalidAspectConfigurationException>();
        }

        [Test]
        public void IfVoidEmptyMethodUnorderedAspectMixedWithOrderedAspectIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(_testType.FullName + ".VoidEmptyMethodUnorderedAspectMixedWithOrderedAspect");

            // Act
            weaver.Weave(WeaveDll);

            // Arrange
            AssertRunPeVerify();
        }
    }
}