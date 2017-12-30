using System;
using System.Collections.Generic;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.Unified.OnEntry
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
            weaver.Weave(Weave.DllPath);

            // Arrange
            AssertRunPeVerify();
            AssertUnifiedMethod(weaver.LastWeavedMethod);
        }

        [Fact]
        public void IfIntMethodIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(_testType.FullName + ".IntMethod");

            // Act
            weaver.Weave(Weave.DllPath);

            // Arrange
            AssertRunPeVerify();
            AssertUnifiedMethod(weaver.LastWeavedMethod);
        }

        [Fact]
        public void IfVoidThrowMethodIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(_testType.FullName + ".VoidThrowMethod");

            // Act
            weaver.Weave(Weave.DllPath);

            // Arrange
            AssertRunPeVerify();
            AssertUnifiedMethod(weaver.LastWeavedMethod, true);
        }

        [Fact]
        public void IfIntMethodIntWithMultipleReturnIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(typeof (TestMethods).FullName + ".IntMethodIntWithMultipleReturn");

            // Act
            weaver.Weave(Weave.DllPath);

            // Arrange
            AssertRunPeVerify();
            AssertUnifiedMethod(weaver.LastWeavedMethod);
        }

        [Fact]
        public void IfVoidThrowMethodTryCatchIsWeaved_ThenPeVerifyShouldBeOk()
        {
            // Arrange
            var weaver = new ModuleWeaver();
            weaver.AddMethodFilter(typeof(TestMethods).FullName + ".VoidThrowMethodTryCatch");

            // Act
            weaver.Weave(Weave.DllPath);

            // Arrange
            AssertRunPeVerify();
            AssertUnifiedMethod(weaver.LastWeavedMethod);
        }

        private static void AssertUnifiedMethod(MethodDefinition method)
        {
            AssertUnifiedMethod(method, false);
        }

        private static void AssertUnifiedMethod(MethodDefinition method, bool methodThrows)
        {
            var allowedLoadOpCodes = new List<OpCode>
            {
                OpCodes.Ldloc_0,
                OpCodes.Ldloc_S,
            };

            var allowedStoreOpCodes = new List<OpCode>
            {
                OpCodes.Stloc_0,
                OpCodes.Stloc_S,
            };

            var instructions = method.Body.Instructions;
            instructions[0].OpCode.Should().Be(OpCodes.Nop);

            var lastIndex = instructions.Count - 1;
            instructions[lastIndex].OpCode.Should().Be(methodThrows ? OpCodes.Throw : OpCodes.Ret);
            if (method.ReturnType.Name == "Void")
            {
                if (methodThrows)
                {
                    instructions[lastIndex - 1].OpCode.Should().Match(x => allowedLoadOpCodes.Contains((OpCode) x));
                    instructions[lastIndex - 6].OpCode.Should().Match(x => allowedLoadOpCodes.Contains((OpCode) x));
                    instructions[lastIndex - 7].OpCode.Should().Match(x => allowedStoreOpCodes.Contains((OpCode) x));
                    instructions[lastIndex - 8].OpCode.Should().Be(OpCodes.Nop);
                }
                else
                {
                    instructions[lastIndex - 1].OpCode.Should().Be(OpCodes.Nop);
                    instructions[lastIndex - 2].OpCode.Should().Be(OpCodes.Nop);
                }
            }
            else
            {
                instructions[lastIndex - 1].OpCode.Should().Match(x => AllLdLocOpCodes.Contains((OpCode) x));
                instructions[lastIndex - 2].OpCode.Should().Be(OpCodes.Nop);
                instructions[lastIndex - 3].OpCode.Should().Match(x => AllStLocOpCodes.Contains((OpCode) x));
                instructions[lastIndex - 4].OpCode.Should().Be(OpCodes.Nop);
            }
        }
    }
}