using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MethodBoundaryAspect.Fody
{
    public class AsyncMethodWeaver : MethodWeaver
    {
        private readonly MethodDefinition _moveNext;
        private readonly MethodInfoCompileTimeWeaver _methodInfoCompileTimeWeaver;

        private readonly VariableDefinition _stateMachineLocal;
        private readonly TypeReference _stateMachine;

        private Instruction _setupPointer;
        private FieldReference _executionArgsField;

        public AsyncMethodWeaver(
            ModuleDefinition module,
            MethodDefinition method,
            MethodDefinition moveNext,
            IList<AspectData> aspects,
            MethodInfoCompileTimeWeaver methodInfoCompileTimeWeaver) :
            base(module, method, aspects, methodInfoCompileTimeWeaver)
        {
            _moveNext = moveNext;
            _methodInfoCompileTimeWeaver = methodInfoCompileTimeWeaver;

            var instructions = _ilProcessor.Body.Instructions;
            if (instructions.Count < 2)
                throw new InvalidOperationException($"Async state machine on {method.FullName} not in expected configuration.");

            _stateMachineLocal = method.Body.Variables.Single(v => v.VariableType.Resolve() == _moveNext.DeclaringType);
            _stateMachine = _stateMachineLocal.VariableType;

            // If state machine is a reference type, then we need to insert our
            // instructions after it is newed up and stored so that we can access
            // the instance.
            if (!_stateMachine.IsValueType)
            {
                if (instructions[0].OpCode != OpCodes.Newobj)
                    throw new InvalidOperationException($"Async state machine on {method.FullName} does not create expected state machine. Opcode = {instructions[0].OpCode}");

                if (!(instructions[0].Operand is MethodReference mr) || mr.DeclaringType?.Resolve()?.Interfaces?[0]?.InterfaceType?.FullName != typeof(IAsyncStateMachine).FullName)
                    throw new InvalidOperationException($"Async state machine on {method.FullName} does not create correct state machine type.");

                var stlocOpcodes = new[]
                {
                    OpCodes.Stloc,
                    OpCodes.Stloc_0,
                    OpCodes.Stloc_1,
                    OpCodes.Stloc_2,
                    OpCodes.Stloc_3,
                    OpCodes.Stloc_S
                };

                if (!stlocOpcodes.Contains(instructions[1].OpCode))
                    throw new InvalidOperationException($"Async state machine on {method.FullName} not stored in expected manner.");

                _setupPointer = instructions[1];
            }
            // If state machine is a value type, it will have default value
            // and be non-nullable, so we can just insert our instructions
            // at the beginning of the method without worrying about NullReferenceException.
            else
                _setupPointer = null;
        }

        protected override void Setup() { }

        protected override void HandleBody(
            NamedInstructionBlockChain arguments,
            VariableDefinition returnValue,
            out Instruction instructionCallStart,
            out Instruction instructionCallEnd)
        {
            instructionCallEnd = _ilProcessor.Body.Instructions.Last();
            if (instructionCallEnd.OpCode == OpCodes.Ret)
            {
                _ilProcessor.Remove(_ilProcessor.Body.Instructions.Last());

                if (returnValue != null)
                    _ilProcessor.Append(Instruction.Create(OpCodes.Stloc, returnValue));
            }
            else
                Debug.Assert(false, "There should always be an unweaved portion of an async method.");

            instructionCallStart = _ilProcessor.Body.Instructions.First();
            instructionCallEnd = _ilProcessor.Body.Instructions.Last();
        }

        protected override void AddToSetup(InstructionBlockChain chain)
        {
            if (_setupPointer == null)
                chain.Prepend(_ilProcessor);
            else
                chain.InsertAfter(_setupPointer, _ilProcessor);

            _setupPointer = chain.Last;
        }

        protected override void Finish() { }

        protected override void WeaveMethodExecutionArgs(NamedInstructionBlockChain arguments)
        {
            var executionArgs = _creator.CreateMethodExecutionArgsInstance(
                arguments,
                _aspects[0].Info.AspectAttribute.AttributeType,
                _method,
                _methodInfoCompileTimeWeaver);

            _executionArgsField = _module.ImportReference(_stateMachine.AddPublicInstanceField(executionArgs.Variable.VariableType));
            executionArgs.Add(new InstructionBlock("", Instruction.Create(OpCodes.Ldloc, executionArgs.Variable)));

            var field = new FieldPersistable(new VariablePersistable(_stateMachineLocal), _executionArgsField);
            var instructions = field.Store(executionArgs.Flatten(), _module.ImportReference(typeof(void)));

            var chain = new InstructionBlockChain();
            chain.Add(instructions);
            AddToSetup(chain);
            ExecutionArgs = field;
        }

        private Instruction GetFirstInstructionToSetException(ExceptionHandler handler, out MethodReference setResultMethod,
            out InstructionBlock loadBuilder)
        {
            var ret = handler.HandlerEnd;
            var leave = ret.Previous;
            var nop = leave.Previous;
            var setException = (nop.OpCode == OpCodes.Nop ? nop.Previous : nop); // The nop is only in Debug mode.
            var setExceptionMethod = (MethodReference)setException.Operand;
            if (setExceptionMethod.DeclaringType is GenericInstanceType setExceptionType)
            {
                var setResultMethodRef = setExceptionType.Resolve().Methods.FirstOrDefault(m => m.Name == "SetResult");
                setResultMethod = _module.ImportReference(setResultMethodRef);
                setResultMethod.DeclaringType = setExceptionType;
            }
            else
            {
                setResultMethod = _module.ImportReference(setExceptionMethod.DeclaringType.Resolve().Methods.FirstOrDefault(m => m.Name == "SetResult"));
            }
            var ldlocException = setException.Previous;
            var ldfldaBuilder = ldlocException.Previous;
            var ldarg_0 = ldfldaBuilder.Previous;
            loadBuilder = new InstructionBlock("Load Builder", ldarg_0, ldfldaBuilder);
            return ldarg_0;
        }

        protected override void WeaveOnException(IList<AspectData> allAspects, Instruction instructionCallStart, Instruction instructionCallEnd, Instruction instructionAfterCall, IPersistable returnValue)
        {
            var handler = _moveNext.Body.ExceptionHandlers.FirstOrDefault(IsStateMachineCatchBlock);
            if (handler == null)
                throw new InvalidOperationException($"Async state machine for {_method.FullName} did not catch exceptions in the expected way.");
            var exceptionLocal = handler.HandlerStart.GetLocalStoredByInstruction(_moveNext.Body.Variables);
            Instruction firstInstructionToSetException = GetFirstInstructionToSetException(handler, out var setResultMethod, out var loadBuilder);
            Instruction exceptionHandlerCurrent = firstInstructionToSetException.Previous; // Need to start inserting before SetException
            Instruction retInstruction = handler.HandlerEnd;
            var processor = _moveNext.Body.GetILProcessor();
            Instruction gotoSetException = Instruction.Create(OpCodes.Br, firstInstructionToSetException);
            new InstructionBlock("else", gotoSetException).InsertAfter(exceptionHandlerCurrent, processor);

            // Need to replace leave.s with leave since we are adding instructions
            // between here and the destination which may invalidate short form labels.
            for (int i = 0; _moveNext.Body.Instructions[i] != handler.HandlerStart; ++i)
            {
                var inst = _moveNext.Body.Instructions[i];
                if (inst.OpCode == OpCodes.Leave_S)
                {
                    inst.OpCode = OpCodes.Leave;
                }
            }

            foreach (var onExceptionAspect in allAspects
                .Where(a => (a.AspectMethods & AspectMethods.OnException) != 0)
                .Reverse()
                .OfType<AspectDataOnAsyncMethod>())
            {
                if (HasMultipleAspects)
                {
                    var load = onExceptionAspect.LoadTagInMoveNext(ExecutionArgs);
                    exceptionHandlerCurrent = load.InsertAfter(exceptionHandlerCurrent, processor);
                }

                var callAspectOnException = onExceptionAspect.CallOnExceptionInMoveNext(ExecutionArgs, exceptionLocal);

                if (setResultMethod.Parameters.Count == 1)
                    returnValue = new VariablePersistable(new InstructionBlockCreator(_moveNext, new ReferenceFinder(_module)).CreateVariable(
                        ((GenericInstanceType)setResultMethod.DeclaringType).GenericArguments[0]));

                var thenBody = new InstructionBlockChain();
                if (setResultMethod.Parameters.Count == 1)
                    thenBody.Add(_creator.ReadReturnValue(onExceptionAspect.GetMoveNextExecutionArgs(ExecutionArgs), returnValue));
                thenBody.Add(loadBuilder.Clone());
                if (setResultMethod.Parameters.Count == 1)
                    thenBody.Add(returnValue.Load(false, false));
                thenBody.Add(new InstructionBlock("Call SetResult", Instruction.Create(OpCodes.Call, setResultMethod)));
                thenBody.Add(new InstructionBlock("Leave peacefully", Instruction.Create(OpCodes.Leave, handler.HandlerEnd)));

                var nop = Instruction.Create(OpCodes.Nop);
                callAspectOnException.Add(_creator.IfFlowBehaviorIsAnyOf(
                    new InstructionBlockCreator(_moveNext, new ReferenceFinder(_module)).CreateVariable,
                    onExceptionAspect.GetMoveNextExecutionArgs(ExecutionArgs),
                    nop,
                    thenBody,
                    1, 3));
                callAspectOnException.Add(new InstructionBlock("", nop));
                callAspectOnException.InsertAfter(exceptionHandlerCurrent, processor);
                exceptionHandlerCurrent = callAspectOnException.Last;
            }
        }

        static bool IsStateMachineCatchBlock(ExceptionHandler handler)
        {
            for (var i = handler.HandlerStart; i != handler.HandlerEnd; i = i.Next)
            {
                if (i.OpCode != OpCodes.Ldfld && i.OpCode != OpCodes.Ldflda)
                    continue;

                if (i.Operand is FieldReference field && field.FieldType.FullName.StartsWith(typeof(AsyncTaskMethodBuilder).FullName))
                    return true;
            }
            return false;
        }
    }
}
