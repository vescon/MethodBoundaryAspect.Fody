using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.Ordering;
using MethodBoundaryAspect.Fody.UnitTests.NetFramework.Unified;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.MultipleAspects.FailTests
{
    public class Tests : UnifiedWeaverTestBase
    {
        private readonly Type _testType = typeof (TestFailMethods);

        [Fact]
        public void IfVoidEmptyMethodMethodIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(_testType.FullName + ".VoidEmptyMethod");

            // Act
            Action call = () => weaver.Weave(Weave.DllPath, ModuleHelper.AssemblyResolver);

            // Arrange
            call.Should().Throw<InvalidAspectConfigurationException>();
        }

        [Fact]
        public void IfVoidEmptyMethodUnorderedAspectMixedWithOrderedAspectIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(_testType.FullName + ".VoidEmptyMethodUnorderedAspectMixedWithOrderedAspect");

            // Act
            weaver.Weave(Weave.DllPath, ModuleHelper.AssemblyResolver);

            // Arrange
            AssertRunPeVerify(weaver);
        }
    }
}