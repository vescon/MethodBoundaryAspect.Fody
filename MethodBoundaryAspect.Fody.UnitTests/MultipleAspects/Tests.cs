using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Unified;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NUnit.Framework;

namespace MethodBoundaryAspect.Fody.UnitTests.MultipleAspects
{
    [TestFixture]
    public class Tests : UnifiedWeaverTestBase
    {
        private readonly Type _testType = typeof (TestMethods);

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
            weaver.Weave(WeaveDll);

            // Arrange
            var peVerifyResult = RunPeVerify();
            peVerifyResult.Should().Be(0);

            AssertUnifiedMethod(weaver.LastWeavedMethod);
        }

        [Test]
        public void IfIntMethodIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(_testType.FullName + ".IntMethod");

            // Act
            weaver.Weave(WeaveDll);

            // Arrange
            var peVerifyResult = RunPeVerify();
            peVerifyResult.Should().Be(0);

            AssertUnifiedMethod(weaver.LastWeavedMethod);
        }

        [Test]
        public void IfVoidThrowMethodIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(_testType.FullName + ".VoidThrowMethod");

            // Act
            weaver.Weave(WeaveDll);

            // Arrange
            var peVerifyResult = RunPeVerify();
            peVerifyResult.Should().Be(0);

            AssertUnifiedMethod(weaver.LastWeavedMethod);
        }

        [Test]
        public void IfIntMethodIntWithMultipleReturnIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(typeof (TestMethods).FullName + ".IntMethodIntWithMultipleReturn");

            // Act
            weaver.Weave(WeaveDll);

            // Arrange
            var peVerifyResult = RunPeVerify();
            peVerifyResult.Should().Be(0);

            AssertUnifiedMethod(weaver.LastWeavedMethod);
        }

        [Test]
        public void IfVoidThrowMethodTryCatchIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(typeof (TestMethods).FullName + ".VoidThrowMethodTryCatch");

            // Act
            weaver.Weave(WeaveDll);

            // Arrange
            var peVerifyResult = RunPeVerify();
            peVerifyResult.Should().Be(0);

            AssertUnifiedMethod(weaver.LastWeavedMethod);
        }

        private static void AssertUnifiedMethod(MethodDefinition method)
        {
            var instructions = method.Body.Instructions;
            instructions[0].OpCode.Should().Be(OpCodes.Nop);

            var lastIndex = instructions.Count - 1;
            instructions[lastIndex].OpCode.Should().Be(OpCodes.Ret);
            if (method.ReturnType.Name == "Void")
            {
                instructions[lastIndex - 4].OpCode.Should().Be(OpCodes.Nop);
                instructions[lastIndex - 5].OpCode.Should().Be(OpCodes.Nop);
            }
            else
            {
                instructions[lastIndex - 1].OpCode.Should().Match(x => AllLdLocOpCodes.Contains((OpCode)x));

                instructions[lastIndex - 9].OpCode.Should().Be(OpCodes.Nop);
                instructions[lastIndex - 11].OpCode.Should().Be(OpCodes.Nop);
            }
        }
    }
}