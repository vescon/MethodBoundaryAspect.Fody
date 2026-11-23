using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// OnException is woven into the catch block of MoveNext which calls SetException of the async method builder
    /// of the compiler. For async methods with another builder (e.g. UniTask) the build failed halfway after
    /// changing the method. Now these methods are not touched and a warning is written.
    /// </summary>
    public class UnsupportedAsyncMethodTests
    {
        private readonly List<string> _warnings = new List<string>();
        private readonly ModuleWeaver _weaver;

        public UnsupportedAsyncMethodTests()
        {

            _weaver = new ModuleWeaver
            {
                ModuleDefinition = WeaverTestHelper.ReadModule(typeof(CustomTaskMethods))
            }.CaptureWarnings(_warnings);
            _weaver.AddClassFilter(typeof(CustomTaskMethods).FullName);
        }

        [Fact]
        public void IfOnExceptionIsUsedWithAnotherAsyncMethodBuilder_ThenTheMethodIsNotWeavedAndAWarningIsWritten()
        {
            _weaver.Execute();

            _warnings.Should().ContainSingle()
                .Which.Should().Contain(nameof(CustomTaskMethods.WithOnException))
                .And.Contain("is not woven")
                .And.Contain("OnException")
                .And.Contain("[DisableWeaving]");
        }

        [Fact]
        public void IfOnExceptionIsUsedWithAnotherAsyncMethodBuilder_ThenTheMethodIsNotChanged()
        {
            _weaver.Execute();

            var type = _weaver.ModuleDefinition.GetType(typeof(CustomTaskMethods).FullName);
            var kickoff = type.Methods.Single(m => m.Name == nameof(CustomTaskMethods.WithOnException));
            var stateMachine = (TypeDefinition)kickoff.CustomAttributes
                .Single(a => a.AttributeType.Name == "AsyncStateMachineAttribute").ConstructorArguments[0].Value;

            GetReferencedTypeNames(kickoff).Should().NotContain(typeof(OnlyOnExceptionAspect).FullName);
            stateMachine.Fields.Select(f => f.FieldType.FullName).Should().NotContain(typeof(OnlyOnExceptionAspect).FullName);
            stateMachine.Fields.Select(f => f.FieldType.Name).Should().NotContain("MethodExecutionArgs");
            type.Methods.Select(m => m.Name).Should().NotContain("$_executor_" + nameof(CustomTaskMethods.WithOnException));
        }

        [Fact]
        public void IfNoAspectMethodIsOverridden_ThenAnAsyncMethodOfAnUnknownStructureIsLeftAlone()
        {
            var weaver = new ModuleWeaver
            {
                ModuleDefinition = WeaverTestHelper.ReadModule(typeof(AsyncWithoutAspectMethods))
            };
            weaver.AddClassFilter(typeof(AsyncWithoutAspectMethods).FullName);
            var kickoff = weaver.ModuleDefinition.GetType(typeof(AsyncWithoutAspectMethods).FullName).Methods
                .Single(m => m.Name == nameof(AsyncWithoutAspectMethods.Async));
            // the state machine is no longer created in the way the async weaver expects
            kickoff.Body.Instructions.RemoveAt(0);
            var instructionCount = kickoff.Body.Instructions.Count;

            Action weave = () => weaver.Execute();

            weave.Should().NotThrow();
            kickoff.Body.Instructions.Should().HaveCount(instructionCount);
        }

        [Fact]
        public void IfTheCatchBlockOfMoveNextIsAsExpected_ThenOnExceptionIsWeaved()
        {
            var warnings = new List<string>();
            var weaver = CreateWeaverForAsyncWithOnException(warnings, out _);

            weaver.Execute();

            warnings.Should().BeEmpty();
            weaver.TotalWeavedMethods.Should().Be(1);
        }

        [Fact]
        public void IfTheCatchBlockOfMoveNextLoadsTheBuilderInAnotherWay_ThenTheMethodIsNotWeavedAndAWarningIsWritten()
        {
            var warnings = new List<string>();
            var weaver = CreateWeaverForAsyncWithOnException(warnings, out var moveNext);
            // "ldarg.0; ldflda builder; ldloc exception; call SetException" is no longer what precedes the call
            var setException = moveNext.Body.Instructions.Single(i => i.Operand is MethodReference m && m.Name == "SetException");
            moveNext.Body.GetILProcessor().InsertBefore(setException, Instruction.Create(OpCodes.Nop));
            var instructionCount = moveNext.Body.Instructions.Count;

            weaver.Execute();

            warnings.Should().ContainSingle()
                .Which.Should().Contain(nameof(AsyncWithOnException.Async))
                .And.Contain("is not woven")
                .And.Contain("expected way");
            moveNext.Body.Instructions.Should().HaveCount(instructionCount);
            weaver.TotalWeavedMethods.Should().Be(0);
        }

        private static ModuleWeaver CreateWeaverForAsyncWithOnException(List<string> warnings, out MethodDefinition moveNext)
        {
            var weaver = new ModuleWeaver
            {
                ModuleDefinition = WeaverTestHelper.ReadModule(typeof(AsyncWithOnException))
            }.CaptureWarnings(warnings);
            weaver.AddClassFilter(typeof(AsyncWithOnException).FullName);

            var kickoff = weaver.ModuleDefinition.GetType(typeof(AsyncWithOnException).FullName).Methods
                .Single(m => m.Name == nameof(AsyncWithOnException.Async));
            var stateMachine = (TypeDefinition)kickoff.CustomAttributes
                .Single(a => a.AttributeType.Name == "AsyncStateMachineAttribute").ConstructorArguments[0].Value;
            moveNext = stateMachine.Methods.Single(m => m.Name == "MoveNext");
            return weaver;
        }

        [Fact]
        public void IfOnlyOnEntryIsUsedWithAnotherAsyncMethodBuilder_ThenTheMethodIsWeaved()
        {
            _weaver.Execute();

            var type = _weaver.ModuleDefinition.GetType(typeof(CustomTaskMethods).FullName);
            var kickoff = type.Methods.Single(m => m.Name == nameof(CustomTaskMethods.WithOnEntry));

            GetReferencedTypeNames(kickoff).Should().Contain(typeof(OnlyOnEntryAspect).FullName);
            _weaver.TotalWeavedMethods.Should().Be(1);
        }

        private static IEnumerable<string> GetReferencedTypeNames(MethodDefinition method)
        {
            return method.Body.Instructions
                .Select(i => i.Operand)
                .Select(operand => operand is MemberReference member ? member.DeclaringType : operand as TypeReference)
                .Where(t => t != null)
                .Select(t => t.FullName)
                .Concat(method.Body.Variables.Select(v => v.VariableType.FullName));
        }
    }
}
