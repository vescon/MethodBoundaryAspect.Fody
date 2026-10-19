using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// The sequence points (breakpoints) of a woven method have to point to instructions of its own body.
    /// The body of a synchronous method moves to the executor method, the kickoff method of an async method loses
    /// its last instruction.
    /// </summary>
    public class DebugInformationTests
    {
        private readonly ModuleWeaver _weaver;
        private readonly TypeDefinition _type;
        private readonly Dictionary<string, List<int>> _originalLines = new Dictionary<string, List<int>>();

        public DebugInformationTests()
        {
            var module = ModuleDefinition.ReadModule(typeof(DebugInformationMethods).Assembly.Location,
                new ReaderParameters
                {
                    AssemblyResolver = ModuleHelper.AssemblyResolver,
                    ReadSymbols = true,
                    SymbolReaderProvider = new DefaultSymbolReaderProvider(throwIfNoSymbol: true)
                });
            _type = module.GetType(typeof(DebugInformationMethods).FullName);

            // the compiler puts no sequence points on the kickoff method of an async method (they are on MoveNext),
            // so add them on its first instruction and on the last one, which is removed by the weaver
            var document = GetMoveNext(nameof(DebugInformationMethods.Async)).DebugInformation.SequencePoints.First().Document;
            foreach (var methodName in new[] { nameof(DebugInformationMethods.Async), nameof(DebugInformationMethods.AsyncWithOnException) })
            {
                var instructions = _type.Methods.Single(m => m.Name == methodName).Body.Instructions;
                foreach (var instruction in new[] { instructions.First(), instructions.Last() })
                {
                    _type.Methods.Single(m => m.Name == methodName).DebugInformation.SequencePoints.Add(
                        new SequencePoint(instruction, document) { StartLine = 1, EndLine = 1, StartColumn = 1, EndColumn = 2 });
                }
            }

            foreach (var method in _type.Methods.Where(m => m.HasBody && m.DebugInformation.HasSequencePoints))
                _originalLines.Add(method.Name, GetLines(method));
            _originalLines.Add("MoveNext of Async", GetLines(GetMoveNext(nameof(DebugInformationMethods.Async))));

            _weaver = new ModuleWeaver { ModuleDefinition = module };
            _weaver.AddClassFilter(typeof(DebugInformationMethods).FullName);
            _weaver.Execute();
        }

        [Fact]
        public void IfASyncMethodIsWeaved_ThenTheExecutorKeepsAllSequencePoints()
        {
            var executor = _type.Methods.Single(m => m.Name == "$_executor_" + nameof(DebugInformationMethods.Sync));

            _originalLines[nameof(DebugInformationMethods.Sync)].Should().NotBeEmpty();
            GetLines(executor).Should().Equal(_originalLines[nameof(DebugInformationMethods.Sync)]);
            AssertSequencePointsBelongToBody(executor);
        }

        [Fact]
        public void IfASyncMethodIsWeaved_ThenTheWeavedMethodHasNoSequencePoints()
        {
            var weavedMethod = _type.Methods.Single(m => m.Name == nameof(DebugInformationMethods.Sync));

            weavedMethod.DebugInformation.HasSequencePoints.Should().BeFalse(
                "the instructions of the sequence points moved to the executor");
        }

        [Theory]
        [InlineData(nameof(DebugInformationMethods.Async))]
        [InlineData(nameof(DebugInformationMethods.AsyncWithOnException))]
        public void IfAnAsyncMethodIsWeaved_ThenTheSequencePointOfTheRemovedInstructionIsRemoved(string methodName)
        {
            var kickoff = _type.Methods.Single(m => m.Name == methodName);

            // the one on the first instruction stays, the one on the last instruction (ret) is removed with it
            kickoff.DebugInformation.SequencePoints.Should().ContainSingle();
            AssertSequencePointsBelongToBody(kickoff);
        }

        [Fact]
        public void IfAnAsyncMethodIsWeaved_ThenTheSequencePointsOfMoveNextAreKept()
        {
            var moveNext = GetMoveNext(nameof(DebugInformationMethods.Async));

            GetLines(moveNext).Should().Equal(_originalLines["MoveNext of Async"]);
        }

        private MethodDefinition GetMoveNext(string asyncMethodName) =>
            _type.NestedTypes.Single(t => t.Name.StartsWith("<" + asyncMethodName + ">d")).Methods.Single(m => m.Name == "MoveNext");

        private static List<int> GetLines(MethodDefinition method) =>
            method.DebugInformation.SequencePoints.Select(sp => sp.StartLine).ToList();

        private static void AssertSequencePointsBelongToBody(MethodDefinition method)
        {
            // Cecil computes the offsets of the instructions when it writes the method, so the weaver has done it
            var instructionOffsets = method.Body.Instructions.Select(i => i.Offset).ToList();
            instructionOffsets.Should().OnlyHaveUniqueItems("the offsets have to be computed");

            var offsets = method.DebugInformation.SequencePoints.Select(sp => sp.Offset).ToList();
            offsets.Should().OnlyHaveUniqueItems();
            offsets.Should().OnlyContain(offset => instructionOffsets.Contains(offset));
        }
    }
}
