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

        private readonly VariableDefinition _stateMachineLocal;
        private readonly TypeReference _stateMachine;

        private Instruction _setupPointer;
        private FieldReference _executionArgsField;

        public AsyncMethodWeaver(
            ModuleDefinition module,
            MethodDefinition method,
            MethodDefinition moveNext,
            IList<AspectData> aspects,
            MethodInfoCompileTimeWeaver methodInfoCompileTimeWeaver,
            ExecutionArgsUsage executionArgsUsage = ExecutionArgsUsage.All) :
            base(module, method, aspects, methodInfoCompileTimeWeaver, executionArgsUsage)
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

                if (!(instructions[0].Operand is MethodReference mr)
                    || mr.DeclaringType?.Resolve()?.Interfaces.Any(i => i.InterfaceType.FullName == typeof(IAsyncStateMachine).FullName) != true)
                    throw new InvalidOperationException($"Async state machine on {method.FullName} does not create correct state machine type.");

                if (!IsStloc(instructions[1]))
                    throw new InvalidOperationException($"Async state machine on {method.FullName} not stored in expected manner.");

                _setupPointer = instructions[1];
            }
            // If state machine is a value type, it will have default value
            // and be non-nullable, so we can just insert our instructions
            // at the beginning of the method without worrying about NullReferenceException.
            // Visual Basic explicitly initializes the state machine first (ldloca, initobj),
            // so we have to insert our instructions after it or they would be overwritten.
            else if (instructions[1].OpCode == OpCodes.Initobj
                     && instructions[1].Operand is TypeReference initType
                     && initType.Resolve() == _moveNext.DeclaringType)
                _setupPointer = instructions[1];
            else
                _setupPointer = null;
        }

        /// <summary>
        /// Checks, before the method is touched, if the structure the weaver relies on is there. The kickoff method
        /// of an async method with another async method builder than the ones of the compiler (e.g. UniTask) may
        /// differ, and the weaver would fail halfway after changing the method.
        /// </summary>
        /// <returns>The reason why the method can't be woven, null if it can.</returns>
        public static string GetUnsupportedReason(MethodDefinition method, MethodDefinition moveNext, bool weavesOnException)
        {
            var instructions = method.Body.Instructions;
            if (instructions.Count < 2)
                return "the state machine is not created in the expected way";

            var stateMachineLocal = method.Body.Variables.SingleOrDefault(v => v.VariableType.Resolve() == moveNext.DeclaringType);
            if (stateMachineLocal == null)
                return "the state machine is not stored in a local variable";

            if (!stateMachineLocal.VariableType.IsValueType && (instructions[0].OpCode != OpCodes.Newobj || !IsStloc(instructions[1])))
                return "the state machine is not created and stored in the expected way";

            if (!weavesOnException)
                return null;

            // OnException is woven into the catch block of MoveNext which completes the async method builder with the exception
            var handler = moveNext.Body.ExceptionHandlers.FirstOrDefault(IsStateMachineCatchBlock);
            if (handler == null)
                return "OnException is used, but the async method builder of the method is not one of the builders of the compiler (e.g. a task-like type with its own builder like UniTask), no catch block of MoveNext completes it with the exception";

            var handlerInstructions = GetHandlerInstructions(handler).ToList();
            var setException = handlerInstructions.FirstOrDefault(IsBuilderSetException);
            if (setException == null || !handlerInstructions.Any(IsStloc) || setException.Previous?.Previous?.Previous == null)
                return "OnException is used, but the catch block of MoveNext doesn't set the exception in the expected way";

            return null;
        }

        protected override void Setup() { }

        protected override void HandleBody(
            NamedInstructionBlockChain arguments,
            VariableDefinition returnValue,
            out Instruction instructionCallStart,
            out Instruction instructionCallEnd)
        {
            if (AllowChangingInputArguments)
                AddToSetup(CreateWriteChangedArgumentsToParameters(arguments));

            instructionCallEnd = _ilProcessor.Body.Instructions.Last();
            if (instructionCallEnd.OpCode == OpCodes.Ret)
            {
                // the instruction is removed, so is its sequence point
                var sequencePoint = _method.DebugInformation.GetSequencePoint(instructionCallEnd);
                if (sequencePoint != null)
                    _method.DebugInformation.SequencePoints.Remove(sequencePoint);

                _ilProcessor.Remove(_ilProcessor.Body.Instructions.Last());

                if (returnValue != null)
                    _ilProcessor.Append(Instruction.Create(OpCodes.Stloc, returnValue));
            }
            else
                Debug.Assert(false, "There should always be an unweaved portion of an async method.");

            instructionCallStart = _ilProcessor.Body.Instructions.First();
            instructionCallEnd = _ilProcessor.Body.Instructions.Last();
        }

        // The compiler copies the parameters into the state machine after the code inserted by the weaver, so the
        // changed arguments reach it if they are written back to the parameters (async methods have no ref, out or in parameters).
        private InstructionBlockChain CreateWriteChangedArgumentsToParameters(NamedInstructionBlockChain arguments)
        {
            var instructions = new List<Instruction>();
            foreach (var parameter in _method.Parameters)
            {
                var element = new ArrayElementLoadable(arguments.Variable, parameter.Index, parameter, _ilProcessor, _creator);
                instructions.AddRange(element.Load(false, true).Instructions);
                instructions.Add(_ilProcessor.Create(OpCodes.Starg, parameter));
            }

            var chain = new InstructionBlockChain();
            chain.Add(new InstructionBlock("Write the changed arguments to the parameters", instructions));
            return chain;
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
            var executionArgs = CreateExecutionArgsInstance(arguments);

            _executionArgsField = _module.ImportReference(_stateMachine.AddPublicInstanceField(executionArgs.Variable.VariableType));
            executionArgs.Add(new InstructionBlock("", Instruction.Create(OpCodes.Ldloc, executionArgs.Variable)));

            var field = new FieldPersistable(new VariablePersistable(_stateMachineLocal), _executionArgsField);
            var instructions = field.Store(executionArgs.Flatten(), _module.TypeSystem.Void);

            var chain = new InstructionBlockChain();
            chain.Add(instructions);
            AddToSetup(chain);
            ExecutionArgs = field;
        }

        private static IEnumerable<Instruction> GetHandlerInstructions(ExceptionHandler handler)
        {
            for (var i = handler.HandlerStart; i != handler.HandlerEnd; i = i.Next)
                yield return i;
        }

        private static VariableDefinition GetExceptionLocal(ExceptionHandler handler, Mono.Collections.Generic.Collection<VariableDefinition> locals)
        {
            // C# stores the exception right at the start of the handler (stloc),
            // Visual Basic calls ProjectData.SetProjectError before (dup, call, stloc).
            var storeException = GetHandlerInstructions(handler).FirstOrDefault(IsStloc);
            if (storeException == null)
                throw new InvalidOperationException("Unable to find variable reference for storage.");

            return storeException.GetLocalStoredByInstruction(locals);
        }

        private static bool IsStloc(Instruction i)
        {
            switch (i.OpCode.Code)
            {
                case Code.Stloc:
                case Code.Stloc_S:
                case Code.Stloc_0:
                case Code.Stloc_1:
                case Code.Stloc_2:
                case Code.Stloc_3:
                    return true;
                default:
                    return false;
            }
        }

        private Instruction GetFirstInstructionToSetException(ExceptionHandler handler, out MethodReference setResultMethod,
            out InstructionBlock loadBuilder, out Instruction afterSetException)
        {
            // C# emits "SetException; [nop;] leave" (the nop is only in Debug mode),
            // Visual Basic emits "SetException; [nop;] call ProjectData.ClearProjectError; leave".
            var setException = GetHandlerInstructions(handler).FirstOrDefault(IsBuilderSetException);
            if (setException == null)
                throw new InvalidOperationException($"Async state machine for {_method.FullName} did not set the exception in the expected way.");

            afterSetException = setException.Next;
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

        protected override void WeaveOnException(IList<AspectData> allAspects, Instruction instructionCallStart, Instruction instructionCallEnd, Instruction instructionAfterCall, NamedInstructionBlockChain returnValue)
        {
            var handler = _moveNext.Body.ExceptionHandlers.FirstOrDefault(IsStateMachineCatchBlock);
            if (handler == null)
                throw new InvalidOperationException($"Async state machine for {_method.FullName} did not catch exceptions in the expected way.");
            var exceptionLocal = GetExceptionLocal(handler, _moveNext.Body.Variables);
            Instruction firstInstructionToSetException = GetFirstInstructionToSetException(handler, out var setResultMethod, out var loadBuilder, out var afterSetException);
            Instruction exceptionHandlerCurrent = firstInstructionToSetException.Previous; // Need to start inserting before SetException
            Instruction retInstruction = handler.HandlerEnd;
            var processor = _moveNext.Body.GetILProcessor();
            // The calls of the aspects are wrapped in a try/catch, so an exception thrown by OnException
            // completes the task as faulted with that exception instead of escaping MoveNext (#5).
            var beforeTry = exceptionHandlerCurrent;
            Instruction gotoSetException = Instruction.Create(OpCodes.Leave, firstInstructionToSetException);
            new InstructionBlock("else", gotoSetException).InsertAfter(exceptionHandlerCurrent, processor);

            // Need to replace the short form branches since we are adding instructions
            // between here and the destination which may invalidate short form labels (#135).
            // Widening a branch makes the code larger, so every short branch has to be widened.
            foreach (var inst in _moveNext.Body.Instructions)
            {
                var longForm = GetLongFormBranch(inst.OpCode);
                if (longForm.HasValue)
                    inst.OpCode = longForm.Value;
            }

            var onExceptionAspects = allAspects
                .Where(a => (a.AspectMethods & AspectMethods.OnException) != 0)
                .Reverse()
                .OfType<AspectDataOnAsyncMethod>()
                .ToList();

            // the result of the task if an aspect swallows the exception (FlowBehavior.Continue)
            VariableDefinition result = null;
            TypeReference returnedTaskType = null;
            if (setResultMethod.Parameters.Count == 1)
            {
                var builderType = (GenericInstanceType)setResultMethod.DeclaringType;
                result = new InstructionBlockCreator(_moveNext, new ReferenceFinder(_module)).CreateVariable(builderType.GenericArguments[0]);
                returnedTaskType = GetReturnedTaskType(builderType);

                // ReturnValue holds the task returned to the caller (set before OnExit), it is no result of the task:
                // without a ReturnValue set by an aspect the task completes with the default value
                var clearReturnValue = _creator.ClearMethodExecutionArgsReturnValue(onExceptionAspects[0].GetMoveNextExecutionArgs(ExecutionArgs));
                exceptionHandlerCurrent = clearReturnValue.InsertAfter(exceptionHandlerCurrent, processor);
            }

            foreach (var onExceptionAspect in onExceptionAspects)
            {
                if (HasMultipleAspects)
                {
                    var load = onExceptionAspect.LoadTagInMoveNext(ExecutionArgs);
                    exceptionHandlerCurrent = load.InsertAfter(exceptionHandlerCurrent, processor);
                }

                var callAspectOnException = onExceptionAspect.CallOnExceptionInMoveNext(ExecutionArgs, exceptionLocal);

                var thenBody = new InstructionBlockChain();
                if (result != null)
                {
                    // the method can return the task (and set it as ReturnValue) on another thread after the
                    // ReturnValue was cleared above, e.g. if the exception is thrown after "await Task.Yield()"
                    var executionArgs = onExceptionAspect.GetMoveNextExecutionArgs(ExecutionArgs);
                    thenBody.Add(_creator.ClearMethodExecutionArgsReturnValue(executionArgs, returnedTaskType));
                    thenBody.Add(_creator.ReadReturnValue(executionArgs, result));
                }
                thenBody.Add(loadBuilder.Clone());
                if (result != null)
                    thenBody.Add(new InstructionBlock("Load result", Instruction.Create(OpCodes.Ldloc, result)));
                thenBody.Add(new InstructionBlock("Call SetResult", Instruction.Create(OpCodes.Call, setResultMethod)));
                // Continue with the compiler generated code after SetException (e.g. ProjectData.ClearProjectError in Visual Basic) which leaves the handler.
                thenBody.Add(new InstructionBlock("Leave peacefully", Instruction.Create(OpCodes.Leave, afterSetException)));

                var nop = Instruction.Create(OpCodes.Nop);
                callAspectOnException.Add(_creator.IfFlowBehaviorIsAnyOf(
                    new InstructionBlockCreator(_moveNext, new ReferenceFinder(_module)).CreateVariable,
                    onExceptionAspect.GetMoveNextExecutionArgs(ExecutionArgs),
                    nop,
                    thenBody,
                    FlowBehaviorValues.Continue, FlowBehaviorValues.Return));
                callAspectOnException.Add(new InstructionBlock("", nop));
                callAspectOnException.InsertAfter(exceptionHandlerCurrent, processor);
                exceptionHandlerCurrent = callAspectOnException.Last;
            }

            // catch (Exception e) { exception = e; } -> SetException(e), the remaining aspects are skipped like in synchronous methods.
            var catchOnException = new InstructionBlockChain();
            catchOnException.Add(new InstructionBlock("Replace exception",
                Instruction.Create(OpCodes.Stloc, exceptionLocal),
                Instruction.Create(OpCodes.Leave, firstInstructionToSetException)));
            catchOnException.InsertAfter(gotoSetException, processor);

            // Nested handlers have to be listed before the handlers enclosing them.
            _moveNext.Body.ExceptionHandlers.Insert(_moveNext.Body.ExceptionHandlers.IndexOf(handler), new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                CatchType = _creator.GetExceptionTypeReference(),
                TryStart = beforeTry.Next,
                TryEnd = catchOnException.First,
                HandlerStart = catchOnException.First,
                HandlerEnd = firstInstructionToSetException
            });
        }

        // the task returned to the caller, e.g. Task<TResult> for AsyncTaskMethodBuilder<TResult>
        private TypeReference GetReturnedTaskType(GenericInstanceType builderType)
        {
            var taskType = (GenericInstanceType)builderType.Resolve().Methods.Single(m => m.Name == "get_Task").ReturnType;
            var returnedTaskType = new GenericInstanceType(_module.ImportReference(taskType.ElementType));
            returnedTaskType.GenericArguments.Add(builderType.GenericArguments[0]);
            return returnedTaskType;
        }

        private static OpCode? GetLongFormBranch(OpCode opCode)
        {
            switch (opCode.Code)
            {
                case Code.Br_S: return OpCodes.Br;
                case Code.Brfalse_S: return OpCodes.Brfalse;
                case Code.Brtrue_S: return OpCodes.Brtrue;
                case Code.Beq_S: return OpCodes.Beq;
                case Code.Bge_S: return OpCodes.Bge;
                case Code.Bgt_S: return OpCodes.Bgt;
                case Code.Ble_S: return OpCodes.Ble;
                case Code.Blt_S: return OpCodes.Blt;
                case Code.Bne_Un_S: return OpCodes.Bne_Un;
                case Code.Bge_Un_S: return OpCodes.Bge_Un;
                case Code.Bgt_Un_S: return OpCodes.Bgt_Un;
                case Code.Ble_Un_S: return OpCodes.Ble_Un;
                case Code.Blt_Un_S: return OpCodes.Blt_Un;
                case Code.Leave_S: return OpCodes.Leave;
                default: return null;
            }
        }

        private static bool IsBuilderSetException(Instruction i)
        {
            return i.OpCode == OpCodes.Call
                   && i.Operand is MethodReference m
                   && m.Name == "SetException"
                   && IsAsyncMethodBuilder(m.DeclaringType);
        }

        static bool IsStateMachineCatchBlock(ExceptionHandler handler)
        {
            for (var i = handler.HandlerStart; i != handler.HandlerEnd; i = i.Next)
            {
                if (i.OpCode != OpCodes.Ldfld && i.OpCode != OpCodes.Ldflda)
                    continue;

                if (i.Operand is FieldReference field && IsAsyncMethodBuilder(field.FieldType))
                    return true;
            }
            return false;
        }

        // Matches the builders used by the compiler for "async Task", "async Task<T>", "async void"
        // and "async ValueTask(<T>)" methods, e.g. AsyncTaskMethodBuilder`1 or AsyncVoidMethodBuilder.
        static bool IsAsyncMethodBuilder(TypeReference type)
        {
            if (type.Namespace != typeof(AsyncTaskMethodBuilder).Namespace)
                return false;

            var name = type.Name;
            var arityIndex = name.IndexOf('`');
            if (arityIndex >= 0)
                name = name.Substring(0, arityIndex);

            return name.EndsWith("MethodBuilder") && name.Contains("Async");
        }
    }
}
