using System;
using System.Collections.Generic;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NUnit.Framework;

namespace MethodBoundaryAspect.Fody.UnitTests.Unified.OnException
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
            weaver.AddMethodFilter(_testType.FullName + ".IntMethodIntWithMultipleReturn");

            // Act
            weaver.Weave(WeaveDll);

            // Arrange
            var peVerifyResult = RunPeVerify();
            peVerifyResult.Should().Be(0);

            AssertUnifiedMethod(weaver.LastWeavedMethod);
        }

        private static void AssertUnifiedMethod(MethodDefinition method)
        {
            var allowedLeaveOpCodes = new List<OpCode>
            {
                OpCodes.Leave,
                OpCodes.Leave_S,
            };

            var instructions = method.Body.Instructions;
            instructions[0].OpCode.Should().Be(OpCodes.Nop);

            var lastIndex = instructions.Count - 1;
            instructions[lastIndex].OpCode.Should().Be(OpCodes.Ret);
            if (method.ReturnType.Name == "Void")
            {
                instructions[lastIndex - 2].OpCode.Should().Be(OpCodes.Rethrow);
                instructions[lastIndex - 12].OpCode.Should().Be(OpCodes.Nop);
                instructions[lastIndex - 13].OpCode.Should().Match(x => allowedLeaveOpCodes.Contains((OpCode) x));
            }
            else
            {
                instructions[lastIndex - 1].OpCode.Should().Match(x => AllLdLocOpCodes.Contains((OpCode) x));
                instructions[lastIndex - 2].OpCode.Should().Be(OpCodes.Nop);
                instructions[lastIndex - 3].OpCode.Should().Be(OpCodes.Rethrow);
                instructions[lastIndex - 13].OpCode.Should().Be(OpCodes.Nop);
                instructions[lastIndex - 14].OpCode.Should().Match(x => allowedLeaveOpCodes.Contains((OpCode) x));
            }
        }
    }
}