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
        MethodDefinition _moveNext;
        Instruction _setupPointer;
        FieldReference _executionArgsField;
        VariableDefinition _stateMachineLocal;
        TypeReference _stateMachine;

        public AsyncMethodWeaver(ModuleDefinition module, MethodDefinition method, MethodDefinition moveNext, IList<AspectData> aspects) :
            base(module, method, aspects)
        {
            _moveNext = moveNext;
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

        protected override void HandleBody(VariableDefinition returnValue, out Instruction instructionCallStart, out Instruction instructionCallEnd)
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
                _aspects[0].Info.AspectAttribute.AttributeType);

            _executionArgsField = _module.ImportReference(_stateMachine.AddPublicInstanceField(executionArgs.Variable.VariableType));
            executionArgs.Add(new InstructionBlock("", Instruction.Create(OpCodes.Ldloc, executionArgs.Variable)));

            var field = new FieldPersistable(new VariablePersistable(_stateMachineLocal), _executionArgsField);
            var instructions = field.Store(executionArgs.Flatten(), _module.ImportReference(typeof(void)));

            var chain = new InstructionBlockChain();
            chain.Add(instructions);
            AddToSetup(chain);
            ExecutionArgs = field;
        }

        protected override void WeaveOnException(List<AspectData> onExceptionAspects, Instruction instructionCallStart, Instruction instructionCallEnd, Instruction instructionAfterCall, IPersistable returnValue)
        {
            var handler = _moveNext.Body.ExceptionHandlers.FirstOrDefault(IsStateMachineCatchBlock);
            if (handler == null)
                throw new InvalidOperationException($"Async state machine for {_method.FullName} did not catch exceptions in the expected way.");
            var exceptionLocal = handler.HandlerStart.GetLocalStoredByInstruction(_moveNext.Body.Variables);
            Instruction exceptionHandlerCurrent = handler.HandlerEnd.Previous.Previous; // HandlerEnd is ret. Previous is leave. Need to insert before the leave.
            var processor = _moveNext.Body.GetILProcessor();

            // Need to replace leave.s with leave since we are adding instructions
            // between here and the destination which may invalidate short form labels.
            for (int i = 0; _moveNext.Body.Instructions[i] != handler.HandlerStart; ++i)
            {
                var inst = _moveNext.Body.Instructions[i];
                if (inst.OpCode == OpCodes.Leave_S)
                {
                    _moveNext.Body.Instructions.RemoveAt(i);
                    _moveNext.Body.Instructions.Insert(i, Instruction.Create(OpCodes.Leave, inst.Operand as Instruction));
                }
            }

            foreach (var onExceptionAspect in onExceptionAspects.OfType<AspectDataOnAsyncMethod>())
            {
                if (HasMultipleAspects)
                {
                    var load = onExceptionAspect.LoadTagInMoveNext(ExecutionArgs);
                    exceptionHandlerCurrent = load.InsertAfter(exceptionHandlerCurrent, processor);
                }

                var callAspectOnException = onExceptionAspect.CallOnExceptionInMoveNext(ExecutionArgs, exceptionLocal);
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
