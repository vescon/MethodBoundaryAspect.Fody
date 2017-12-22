using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace MethodBoundaryAspect.Fody
{
    public class MethodBodyPatcher
    {
        private readonly string _methodName;

        private static readonly ISet<OpCode> JumpInstructions = new HashSet<OpCode>
        {
            OpCodes.Beq,
            OpCodes.Beq_S,

            OpCodes.Bge,
            OpCodes.Bge_S,
            OpCodes.Bge_Un,
            OpCodes.Bge_Un_S,

            OpCodes.Bgt,
            OpCodes.Bgt_S,
            OpCodes.Bgt_Un,
            OpCodes.Bgt_Un_S,

            OpCodes.Ble,
            OpCodes.Ble_S,
            OpCodes.Ble_Un,
            OpCodes.Ble_Un_S,

            OpCodes.Blt,
            OpCodes.Blt_S,
            OpCodes.Blt_Un,
            OpCodes.Blt_Un_S,

            OpCodes.Bne_Un,
            OpCodes.Bne_Un_S,

            OpCodes.Br,
            OpCodes.Br_S,

            OpCodes.Brfalse,
            OpCodes.Brfalse_S,

            OpCodes.Brtrue,
            OpCodes.Brtrue_S,

            OpCodes.Leave,
            OpCodes.Leave_S
        };

        private readonly MethodBody _methodBody;
        private readonly ILProcessor _processor;

        private readonly Instruction _realBodyStart;
        private readonly Instruction _realBodyEnd;

        private readonly Instruction _markStart1BeforeCreateArgumentsArray;
        private readonly Instruction _markStart2BeforeCreateMethodExecutionArgs;
        private readonly Instruction _markStart3BeforeOnEntryCall;
        private readonly Instruction _markStart4BeforeRealBodyStartExceptionHandler;
        private readonly Instruction _markEnd1NewRealBodyEnd;
        private readonly Instruction _markEnd2BeforeOnExitCall;
        private readonly Instruction _markRetNew;

        private Instruction _markLeaveTryBlock;
        private Instruction _markExceptionHandlerStart;
        private Instruction _markExceptionHandlerEnd;

        private bool _aspectInstanceCreated;

        public MethodBodyPatcher(string methodName, MethodDefinition method)
        {
            _methodName = methodName;
            _methodBody = method.Body;
            _processor = _methodBody.GetILProcessor();

            _realBodyStart = _methodBody.Instructions.First();
            _realBodyEnd = _methodBody.Instructions.Last();

            _markStart1BeforeCreateArgumentsArray = _processor.Create(OpCodes.Nop);
            _markStart2BeforeCreateMethodExecutionArgs = _processor.Create(OpCodes.Nop);
            _markStart3BeforeOnEntryCall = _processor.Create(OpCodes.Nop);
            _markStart4BeforeRealBodyStartExceptionHandler = _processor.Create(OpCodes.Nop);
            _markEnd1NewRealBodyEnd = _processor.Create(OpCodes.Nop);
            _markEnd2BeforeOnExitCall = _processor.Create(OpCodes.Nop);
            _markRetNew = EndsWithThrow ? _processor.Create(OpCodes.Throw) : _processor.Create(OpCodes.Ret);

            HasMultipleReturnAndEndsWithThrow = EndsWithThrow && _methodBody.Instructions.Any(x => x.OpCode == OpCodes.Ret);
        }

        public bool EndsWithThrow => _realBodyEnd.OpCode.Code == Code.Throw;

        public bool HasMultipleReturnAndEndsWithThrow { get; }

        public void Unify(
            NamedInstructionBlockChain saveReturnValue,NamedInstructionBlockChain loadReturnValue)
        {
            _methodBody.InitLocals = true;

            _processor.InsertBefore(_realBodyStart, _markStart4BeforeRealBodyStartExceptionHandler);
            _processor.InsertBefore(_markStart4BeforeRealBodyStartExceptionHandler, _markStart3BeforeOnEntryCall);
            _processor.InsertBefore(_markStart3BeforeOnEntryCall, _markStart2BeforeCreateMethodExecutionArgs);
            _processor.InsertBefore(_markStart2BeforeCreateMethodExecutionArgs, _markStart1BeforeCreateArgumentsArray);
            _processor.InsertAfter(_realBodyEnd, _markEnd1NewRealBodyEnd);
            _processor.InsertAfter(_markEnd1NewRealBodyEnd, _markEnd2BeforeOnExitCall);
            _processor.InsertAfter(_markEnd2BeforeOnExitCall, _markRetNew);

            if (!EndsWithThrow)
            {
                saveReturnValue.InsertAfter(_markEnd1NewRealBodyEnd, _processor);
                loadReturnValue.InsertAfter(_markEnd2BeforeOnExitCall, _processor);
            }

            FixRealRetsToBranchToNewRealBodyEnd();
            FixCatchHandlersWithNullEnd();
        }

        public void FixThrowAtEndOfRealBody(
            NamedInstructionBlockChain saveThrownException,
            NamedInstructionBlockChain loadThrownException,
            NamedInstructionBlockChain loadThrownException2)
        {
            // store exception from stack
            var retTemp = _processor.Create(OpCodes.Nop);
            _processor.InsertBefore(_realBodyEnd, retTemp);
            var lastInstruction = saveThrownException.InsertAfter(retTemp, _processor);

            // load exception to stack
            loadThrownException.InsertAfter(lastInstruction, _processor);

            // exception will be thrown at old real body end (maybe in try/catch)

            // after "OnException" load exception again to stack
            var retTemp2 = _processor.Create(OpCodes.Nop);
            _processor.Replace(_markRetNew, retTemp2);
            lastInstruction = loadThrownException2.InsertAfter(retTemp2, _processor);

            // then throw again
            _processor.InsertAfter(lastInstruction, _processor.Create(OpCodes.Throw));
        }

        public void ReplaceThrowAtEndOfRealBodyWithReturn()
        {
            var returnInstruction = _processor.Create(OpCodes.Ret);
            _processor.Replace(_markRetNew, returnInstruction);
        }

        private void FixRealRetsToBranchToNewRealBodyEnd()
        {
            var otherRetInstructions = _methodBody.Instructions
                .Where(x => x.OpCode == OpCodes.Ret)
                .Where(x => x != _markRetNew)
                .ToList();

            if (!otherRetInstructions.Any())
                return;

            foreach (var otherRetInstruction in otherRetInstructions)
            {
                var branchInstruction = _processor.Create(OpCodes.Br, _markEnd1NewRealBodyEnd);
                _processor.Replace(otherRetInstruction, branchInstruction);

                FixJumpInstruction(otherRetInstruction, _markEnd1NewRealBodyEnd);
                FixCatchHandlersEnd(otherRetInstruction, _markEnd1NewRealBodyEnd.Previous);
            }
        }

        private void FixJumpInstruction(Instruction oldTarget, Instruction newTarget)
        {
            foreach (var jumpInstruction in _methodBody.Instructions.Where(x => JumpInstructions.Contains(x.OpCode)))
            {
                if (jumpInstruction.Operand != oldTarget)
                    continue;

                jumpInstruction.Operand = newTarget;
            }
        }

        private void FixCatchHandlersWithNullEnd()
        {
            FixCatchHandlersEnd(null, _markEnd2BeforeOnExitCall.Previous);
        }

        private void FixCatchHandlersEnd(Instruction oldEnd, Instruction newEnd)
        {
            var exceptionHandlers = _methodBody.ExceptionHandlers
                .Where(x => x.HandlerEnd == oldEnd)
                .ToList();

            foreach (var exceptionHandler in exceptionHandlers)
                exceptionHandler.HandlerEnd = newEnd;
        }

        public void AddCreateArgumentsArray(NamedInstructionBlockChain createArgumentsArray)
        {
            createArgumentsArray.InsertAfter(_markStart1BeforeCreateArgumentsArray, _processor);
        }

        public void AddCreateMethodExecutionArgs(NamedInstructionBlockChain createMethodExecutionArgsInstance)
        {
            createMethodExecutionArgsInstance.InsertAfter(_markStart2BeforeCreateMethodExecutionArgs, _processor);
        }

        public void AddOnEntryCall(
            NamedInstructionBlockChain createAspectInstance,
            InstructionBlockChain callAspectOnEntry)
        {
            var current = AddCreateAspectInstance(createAspectInstance, _markStart3BeforeOnEntryCall);
            callAspectOnEntry.InsertAfter(current, _processor);
        }

        public void AddOnExitCall(
            NamedInstructionBlockChain createAspectInstance,
            InstructionBlockChain callAspectOnExit,
            InstructionBlockChain setMethodExecutionArgsReturnValue)
        {
            var current = setMethodExecutionArgsReturnValue.InsertAfter(_markEnd2BeforeOnExitCall, _processor);
            current = AddCreateAspectInstance(createAspectInstance, current);
            callAspectOnExit.InsertAfter(current, _processor);
        }

        public void AddOnExceptionCall(
            NamedInstructionBlockChain createAspectInstance,
            InstructionBlockChain callAspectOnException,
            NamedInstructionBlockChain setMethodExecutionArgsExceptionFromStack)
        {
            // add leave
            _markLeaveTryBlock = _processor.Create(OpCodes.Leave, _markEnd2BeforeOnExitCall);
            _processor.InsertBefore(_markEnd2BeforeOnExitCall, _markLeaveTryBlock);

            // add catch handler after leave
            _markExceptionHandlerStart = _processor.Create(OpCodes.Nop);
            _processor.InsertAfter(_markLeaveTryBlock, _markExceptionHandlerStart);
            var current = setMethodExecutionArgsExceptionFromStack.InsertAfter(_markExceptionHandlerStart, _processor);
            current = AddCreateAspectInstance(createAspectInstance, current);
            current = callAspectOnException.InsertAfter(current, _processor);
            _markExceptionHandlerEnd = _processor.Create(OpCodes.Rethrow);
            _processor.InsertAfter(current, _markExceptionHandlerEnd);

            // add exception handler
            _methodBody.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                CatchType = setMethodExecutionArgsExceptionFromStack.TypeReference,
                TryStart = _markStart4BeforeRealBodyStartExceptionHandler,
                TryEnd = _markExceptionHandlerStart,
                HandlerStart = _markExceptionHandlerStart,
                HandlerEnd = _markExceptionHandlerEnd.Next
            });
        }

        public void OptimizeBody()
        {
            _methodBody.OptimizeMacros();

            Trace.WriteLine("Method: " + _methodName);

            Dump("_realBodyStart", _realBodyStart);
            Dump("_realBodyEnd", _realBodyEnd);

            Dump("_markStart1BeforeCreateArgumentsArray", _markStart1BeforeCreateArgumentsArray);
            Dump("_markStart2BeforeCreateMethodExecutionArgs", _markStart2BeforeCreateMethodExecutionArgs);
            Dump("_markStart3BeforeOnEntryCall", _markStart3BeforeOnEntryCall);
            Dump("_markStart4BeforeRealBodyStartExceptionHandler", _markStart4BeforeRealBodyStartExceptionHandler);
            Dump("_markEnd1NewRealBodyEnd", _markEnd1NewRealBodyEnd);
            Dump("_markEnd2BeforeOnExitCall", _markEnd2BeforeOnExitCall);
            Dump("_markRetNew", _markRetNew);

            Dump("_markLeaveTryBlock", _markLeaveTryBlock);
            Dump("_markExceptionHandlerStart", _markExceptionHandlerStart);
            Dump("_markExceptionHandlerEnd", _markExceptionHandlerEnd);
        }

        private Instruction AddCreateAspectInstance(NamedInstructionBlockChain createAspectInstance, Instruction current)
        {
            if (_aspectInstanceCreated)
                return current;

            current = createAspectInstance.InsertAfter(current, _processor);
            _aspectInstanceCreated = true;
            return current;
        }

        private void Dump(string name, Instruction instruction)
        {
            Trace.WriteLine(instruction == null
                ? string.Format("{0} is not set", name)
                : string.Format("{0} is at: {1}", name, _methodBody.Instructions.IndexOf(instruction)));
        }
    }
}