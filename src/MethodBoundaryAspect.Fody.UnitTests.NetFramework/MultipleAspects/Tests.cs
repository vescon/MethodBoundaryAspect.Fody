using System;
using MethodBoundaryAspect.Fody.UnitTests.NetFramework.Unified;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.MultipleAspects
{
    public class Tests : UnifiedWeaverTestBase
    {
        private readonly Type _testType = typeof (TestMethods);
        
        [Fact]
        public void IfVoidEmptyMethodMethodIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(_testType.FullName + ".VoidEmptyMethod");

            // Act
            weaver.Weave(Weave.DllPath, ModuleHelper.AssemblyResolver);

            // Arrange
            AssertRunPeVerify(weaver);
        }

        [Fact]
        public void IfIntMethodIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(_testType.FullName + ".IntMethod");

            // Act
            weaver.Weave(Weave.DllPath, ModuleHelper.AssemblyResolver);

            // Arrange
            AssertRunPeVerify(weaver);
        }

        [Fact]
        public void IfVoidThrowMethodIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(_testType.FullName + ".VoidThrowMethod");

            // Act
            weaver.Weave(Weave.DllPath, ModuleHelper.AssemblyResolver);

            // Arrange
            AssertRunPeVerify(weaver);
        }

        [Fact]
        public void IfIntMethodIntWithMultipleReturnIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(typeof (TestMethods).FullName + ".IntMethodIntWithMultipleReturn");

            // Act
            weaver.Weave(Weave.DllPath, ModuleHelper.AssemblyResolver);

            // Arrange
            AssertRunPeVerify(weaver);
        }

        [Fact]
        public void IfVoidThrowMethodTryCatchIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(typeof (TestMethods).FullName + ".VoidThrowMethodTryCatch");

            // Act
            weaver.Weave(Weave.DllPath, ModuleHelper.AssemblyResolver);

            // Arrange
            AssertRunPeVerify(weaver);
        }
    }
}