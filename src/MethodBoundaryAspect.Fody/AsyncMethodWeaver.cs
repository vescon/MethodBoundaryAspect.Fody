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
            MethodInfoCompileTimeWeaver methodInfoCompileTimeWeaver,
            ExecutionArgsUsage executionArgsUsage = ExecutionArgsUsage.All) :
            base(module, method, aspects, methodInfoCompileTimeWeaver, executionArgsUsage)
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
            // Visual Basic explicitly initializes the state machine first (ldloca, initobj),
            // so we have to insert our instructions after it or they would be overwritten.
            else if (instructions[1].OpCode == OpCodes.Initobj
                     && instructions[1].Operand is TypeReference initType
                     && initType.Resolve() == _moveNext.DeclaringType)
                _setupPointer = instructions[1];
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
                _methodInfoCompileTimeWeaver,
                _executionArgsUsage);

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
            var setException = GetHandlerInstructions(handler).FirstOrDefault(i =>
                i.OpCode == OpCodes.Call
                && i.Operand is MethodReference m
                && m.Name == "SetException"
                && IsAsyncMethodBuilder(m.DeclaringType));
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

        protected override void WeaveOnException(IList<AspectData> allAspects, Instruction instructionCallStart, Instruction instructionCallEnd, Instruction instructionAfterCall, IPersistable returnValue)
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
                // Continue with the compiler generated code after SetException (e.g. ProjectData.ClearProjectError in Visual Basic) which leaves the handler.
                thenBody.Add(new InstructionBlock("Leave peacefully", Instruction.Create(OpCodes.Leave, afterSetException)));

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
