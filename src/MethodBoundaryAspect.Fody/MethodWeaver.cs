using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MethodBoundaryAspect.Fody
{
    public class MethodWeaver
    {
        private MethodDefinition _clonedMethod;

        protected readonly ModuleDefinition _module;
        protected readonly MethodDefinition _method;
        protected readonly InstructionBlockChainCreator _creator;
        protected readonly ILProcessor _ilProcessor;
        protected readonly IList<AspectData> _aspects;
        protected readonly MethodInfoCompileTimeWeaver _methodInfoCompileTimeWeaver;
        protected readonly ExecutionArgsUsage _executionArgsUsage;

        protected bool HasMultipleAspects => _aspects.Count > 1;
        protected bool AllowChangingInputArguments => _aspects.Any(x => x.Info.AllowChangingInputArguments);

        // a return value not used by any aspect doesn't have to be boxed into MethodExecutionArgs.
        // It is also needed if it's only written: it's read back after OnExit, also if the aspect didn't overwrite it
        protected bool ProvidesReturnValue =>
            (_executionArgsUsage & (ExecutionArgsUsage.ReadReturnValue | ExecutionArgsUsage.WriteReturnValue)) != 0;

        // a return value not written by any aspect doesn't have to be read back from MethodExecutionArgs,
        // a skipped method body (FlowBehavior.Return, Continue) returns the default value
        protected bool ReadsBackReturnValue => (_executionArgsUsage & ExecutionArgsUsage.WriteReturnValue) != 0;

        protected IPersistable ExecutionArgs { get; set; }

        public int WeaveCounter { get; private set; }

        public MethodWeaver(
            ModuleDefinition module,
            MethodDefinition method,
            IList<AspectData> aspects,
            MethodInfoCompileTimeWeaver methodInfoCompileTimeWeaver,
            ExecutionArgsUsage executionArgsUsage = ExecutionArgsUsage.All)
        {
            _module = module;
            _method = method;
            _creator = new InstructionBlockChainCreator(method, module);
            _ilProcessor = _method.Body.GetILProcessor();
            _aspects = aspects;
            _methodInfoCompileTimeWeaver = methodInfoCompileTimeWeaver;
            _executionArgsUsage = executionArgsUsage;
        }

        public void Weave()
        {
            if (_aspects.Count == 0)
                return;

            Setup();

            // the arguments array is only created if an aspect reads it or changed arguments are passed to the method
            NamedInstructionBlockChain arguments = null;
            if (AllowChangingInputArguments || (_executionArgsUsage & ExecutionArgsUsage.Arguments) != 0)
            {
                arguments = _creator.CreateMethodArgumentsArray();
                AddToSetup(arguments);
            }

            WeaveMethodExecutionArgs(arguments);

            SetupAspects();

            var hasReturnValue = !InstructionBlockCreator.IsVoid(_method.ReturnType);
            var returnValue = hasReturnValue
                    ? _creator.CreateVariable(_method.ReturnType.WithoutModifiers())
                    : null;

            WeaveOnEntry(arguments, returnValue);
            
            HandleBody(arguments, returnValue?.Variable, out var instructionCallStart, out var instructionCallEnd);

            var instructionAfterCall = WeaveOnExit(hasReturnValue, returnValue);

            HandleReturnValue(hasReturnValue, returnValue);

            if (_aspects.Any(x => (x.AspectMethods & AspectMethods.OnException) != 0))
                WeaveOnException(_aspects, instructionCallStart, instructionCallEnd, instructionAfterCall, returnValue);

            Optimize();

            Finish();

            WeaveCounter++;
        }

        protected virtual void Setup()
        {
            _clonedMethod = CloneMethod(_method);
            _method.DeclaringType.Methods.Add(_clonedMethod);
            ClearMethod(_method);
        }

        private static MethodDefinition CloneMethod(MethodDefinition method)
        {
            var targetMethodName = "$_executor_" + method.Name;
            var isStaticMethod = method.IsStatic;
            var methodAttributes = MethodAttributes.Private;
            if (isStaticMethod)
                methodAttributes |= MethodAttributes.Static;

            var clonedMethod = new MethodDefinition(targetMethodName, methodAttributes, method.ReturnType)
            {
                AggressiveInlining = true, // try to get rid of additional stack frame
                HasThis = method.HasThis,
                ExplicitThis = method.ExplicitThis,
                CallingConvention = method.CallingConvention
            };

            // the settings of the original method stay with its body (NoInlining stays on the original method
            // which calls the executor), e.g. [SkipLocalsInit] and [MethodImpl(MethodImplOptions.NoOptimization)]
            clonedMethod.Body.InitLocals = method.Body.InitLocals;
            clonedMethod.ImplAttributes |= method.ImplAttributes
                                           & (MethodImplAttributes.NoOptimization | MethodImplAttributes.AggressiveOptimization);

            foreach (var parameter in method.Parameters)
                clonedMethod.Parameters.Add(parameter);

            foreach (var variable in method.Body.Variables)
                clonedMethod.Body.Variables.Add(variable);

            foreach (var variable in method.Body.ExceptionHandlers)
                clonedMethod.Body.ExceptionHandlers.Add(variable);

            var targetProcessor = clonedMethod.Body.GetILProcessor();
            foreach (var instruction in method.Body.Instructions)
                targetProcessor.Append(instruction);

            if (method.HasGenericParameters)
            {
                //Contravariant:
                //  The generic type parameter is contravariant. A contravariant type parameter can appear as a parameter type in method signatures.  
                //Covariant:
                //  The generic type parameter is covariant. A covariant type parameter can appear as the result type of a method, the type of a read-only field, a declared base type, or an implemented interface. 
                //DefaultConstructorConstraint:
                //  A type can be substituted for the generic type parameter only if it has a parameterless constructor. 
                //None:
                //  There are no special flags. 
                //NotNullableValueTypeConstraint:
                //  A type can be substituted for the generic type parameter only if it is a value type and is not nullable. 
                //ReferenceTypeConstraint:
                //  A type can be substituted for the generic type parameter only if it is a reference type. 
                //SpecialConstraintMask:
                //  Selects the combination of all special constraint flags. This value is the result of using logical OR to combine the following flags: DefaultConstructorConstraint, ReferenceTypeConstraint, and NotNullableValueTypeConstraint. 
                //VarianceMask:
                //  Selects the combination of all variance flags. This value is the result of using logical OR to combine the following flags: Contravariant and Covariant. 
                foreach (var parameter in method.GenericParameters)
                {
                    // all flags, also those without a type constraint, e.g. "allows ref struct"
                    var clonedParameter = new GenericParameter(parameter.Name, clonedMethod)
                    {
                        Attributes = parameter.Attributes
                    };
                    foreach (var parameterConstraint in parameter.Constraints)
                        clonedParameter.Constraints.Add(parameterConstraint);

                    clonedMethod.GenericParameters.Add(clonedParameter);
                }
            }

            if (method.DebugInformation.HasSequencePoints)
            {
                foreach (var sequencePoint in method.DebugInformation.SequencePoints)
                    clonedMethod.DebugInformation.SequencePoints.Add(sequencePoint);
            }

            clonedMethod.DebugInformation.Scope = new ScopeDebugInformation(method.Body.Instructions.First(), method.Body.Instructions.Last());

            if (method.DebugInformation?.Scope?.Variables != null)
            {
                foreach (var variableDebugInformation in method.DebugInformation.Scope.Variables)
                {
                    clonedMethod.DebugInformation.Scope.Variables.Add(variableDebugInformation);
                }
            }

            if (method.DebugInformation?.Scope?.Scopes != null)
            {
                foreach (var scopeDebugInformation in method.DebugInformation.Scope.Scopes)
                {
                    clonedMethod.DebugInformation.Scope.Scopes.Add(scopeDebugInformation);
                }
            }

            return clonedMethod;
        }

        private static void ClearMethod(MethodDefinition method)
        {
            var body = method.Body;
            body.Variables.Clear();
            body.Instructions.Clear();
            body.ExceptionHandlers.Clear();

            // the instructions moved to the executor method, which got the sequence points
            method.DebugInformation.SequencePoints.Clear();
        }

        protected virtual void AddToSetup(InstructionBlockChain chain)
        {
            chain.Append(_ilProcessor);
        }

        protected virtual void WeaveMethodExecutionArgs(NamedInstructionBlockChain arguments)
        {
            var executionArgs = CreateExecutionArgsInstance(arguments);
            AddToSetup(executionArgs);
            ExecutionArgs = executionArgs;
        }

        protected NamedInstructionBlockChain CreateExecutionArgsInstance(NamedInstructionBlockChain arguments)
        {
            return _creator.CreateMethodExecutionArgsInstance(
                arguments,
                _aspects[0].Info.AspectAttribute.AttributeType,
                _method,
                _methodInfoCompileTimeWeaver,
                _executionArgsUsage);
        }

        private void SetupAspects()
        {
            foreach (var aspect in _aspects)
            {
                var instance = aspect.CreateAspectInstance();
                AddToSetup(instance);

                if (HasMultipleAspects)
                    aspect.EnsureTagStorage();
            }
        }

        private void WeaveOnEntry(NamedInstructionBlockChain arguments, NamedInstructionBlockChain returnValue)
        {
            var allowChangingInputArguments = AllowChangingInputArguments;

            var aspectsWithOnEntry = _aspects
                .Select((asp, index)=> new { aspect = asp, index })
                .Where(x => (x.aspect.AspectMethods & AspectMethods.OnEntry) != 0)
                .ToList();
            foreach (var entry in aspectsWithOnEntry)
            {
                var aspect = entry.aspect;
                var call = _creator.CallAspectOnEntry(aspect, ExecutionArgs);
                AddToSetup(call);

                if (HasMultipleAspects)
                    AddToSetup(_creator.SaveMethodExecutionArgsTagToPersistable(ExecutionArgs, aspect.TagPersistable));
                
                var nopChain = new InstructionBlockChain();
                nopChain.Add(new InstructionBlock(null, Instruction.Create(OpCodes.Nop)));

                var flowChain = new InstructionBlockChain();
                var onExitChain = new InstructionBlockChain();

                if (_method.ReturnType.WithoutModifiers().IsByReference)
                {
                    onExitChain.Add(CreateThrowNotSupported(
                        "Weaving early return from a method with a byref return type is not supported."));
                }
                else
                {
                    for (var i = entry.index-1; i >= 0; --i)
                    {
                        var onExitAspect = _aspects[i];
                        if ((onExitAspect.AspectMethods & AspectMethods.OnExit) == 0) 
                            continue;

                        if (HasMultipleAspects)
                            onExitChain.Add(_creator.LoadMethodExecutionArgsTagFromPersistable(ExecutionArgs, onExitAspect.TagPersistable));
                        onExitChain.Add(_creator.CallAspectOnExit(onExitAspect, ExecutionArgs));
                    }

                    if (allowChangingInputArguments)
                    {
                        // method body is skipped: write ref/out values set by the aspect back to the caller
                        var copyBackBlock = CreateCopyBackByRefArgumentsFromArray(arguments);
                        if (copyBackBlock != null)
                            onExitChain.Add(copyBackBlock);
                    }
                }

                if (returnValue != null)
                {
                    if (ReadsBackReturnValue)
                        onExitChain.Add(_creator.ReadReturnValue(ExecutionArgs, returnValue.Variable));
                    onExitChain.Add(CreateReturnCompletedTaskIfNull(returnValue.Variable));
                    onExitChain.Add(_creator.LoadValueOnStack(returnValue));
                }

                onExitChain.Add(new InstructionBlock("Return", Instruction.Create(OpCodes.Ret)));

                flowChain.Add(_creator.IfFlowBehaviorIsAnyOf(ExecutionArgs, nopChain.First, onExitChain, 3));

                flowChain.Add(nopChain);
                AddToSetup(flowChain);
            }
        }
        
        /// <summary>
        /// A Task or Task&lt;T&gt; is never null for the caller: if the method body is skipped or an exception is
        /// swallowed and no aspect set a ReturnValue, the method returns a completed task with the default result.
        /// </summary>
        private InstructionBlockChain CreateReturnCompletedTaskIfNull(VariableDefinition returnValue)
        {
            var chain = new InstructionBlockChain();
            var returnType = _method.ReturnType.WithoutModifiers();

            MethodReference createTask;
            VariableDefinition defaultResult = null;
            if (returnType.FullName == "System.Threading.Tasks.Task")
            {
                var getCompletedTask = returnType.Resolve()?.Methods
                    .FirstOrDefault(m => m.Name == "get_CompletedTask" && m.IsStatic && m.Parameters.Count == 0);
                if (getCompletedTask == null)
                    return chain;

                createTask = _module.ImportReference(getCompletedTask);
            }
            else if (returnType is GenericInstanceType taskOfResult
                     && taskOfResult.ElementType.FullName == "System.Threading.Tasks.Task`1")
            {
                var fromResult = taskOfResult.ElementType.Resolve()?.Module.GetType("System.Threading.Tasks.Task")?.Methods
                    .FirstOrDefault(m => m.Name == "FromResult" && m.IsStatic && m.HasGenericParameters && m.Parameters.Count == 1);
                if (fromResult == null)
                    return chain;

                var resultType = taskOfResult.GenericArguments[0];
                var fromResultOfResult = new GenericInstanceMethod(_module.ImportReference(fromResult));
                fromResultOfResult.GenericArguments.Add(resultType);
                createTask = fromResultOfResult;
                defaultResult = _creator.CreateVariable(resultType).Variable;
            }
            else
                return chain;

            var end = Instruction.Create(OpCodes.Nop);
            var instructions = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldloc, returnValue),
                Instruction.Create(OpCodes.Brtrue, end)
            };
            if (defaultResult != null)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldloca, defaultResult));
                instructions.Add(Instruction.Create(OpCodes.Initobj, defaultResult.VariableType));
                instructions.Add(Instruction.Create(OpCodes.Ldloc, defaultResult));
            }
            instructions.Add(Instruction.Create(OpCodes.Call, createTask));
            instructions.Add(Instruction.Create(OpCodes.Stloc, returnValue));

            chain.Add(new InstructionBlock("Return a completed task if there is none", instructions));
            chain.Add(new InstructionBlock("End", end));
            return chain;
        }

        // a method with a ref return cannot return a default value (a null reference fails in the caller)
        private InstructionBlock CreateThrowNotSupported(string message, VariableDefinition innerException = null)
        {
            var parameterCount = innerException == null ? 1 : 2;
            var constructor = _module.ImportReference(
                _creator.GetExceptionTypeReference<NotSupportedException>()
                    .Resolve().Methods
                    .First(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == parameterCount
                                && m.Parameters[0].ParameterType.FullName == "System.String"
                                && (parameterCount == 1 || m.Parameters[1].ParameterType.FullName == "System.Exception")));

            var instructions = new List<Instruction> { Instruction.Create(OpCodes.Ldstr, message) };
            if (innerException != null)
                instructions.Add(Instruction.Create(OpCodes.Ldloc, innerException));
            instructions.Add(Instruction.Create(OpCodes.Newobj, constructor));
            instructions.Add(Instruction.Create(OpCodes.Throw));
            return new InstructionBlock("Throw NotSupported", instructions);
        }

        protected virtual void HandleBody(
            NamedInstructionBlockChain arguments,
            VariableDefinition returnValue,
            out Instruction instructionCallStart,
            out Instruction instructionCallEnd)
        {
            VariableDefinition thisVariable = null;
            if (!_method.IsStatic)
            {
                var thisVariableBlock = _creator.CreateThisVariable(_method.DeclaringType);
                thisVariableBlock.Append(_ilProcessor);
                thisVariable = thisVariableBlock.Variable;
            }

            InstructionBlockChain callSourceMethod;

            ILoadable[] args = null;
            var allowChangingInputArguments = AllowChangingInputArguments;
            if (allowChangingInputArguments)
            {
                // get arguments from ExecutionArgs because they could have been changed in aspect code
                // (except ref structs and pointers which cannot be passed via ExecutionArgs)
                var instanceOffset = _method.IsStatic ? 0 : 1;
                args = _method.Parameters
                    .Select((x, i) => x.ParameterType.CannotBeBoxed()
                        ? (ILoadable)new ArgumentLoadable(i + instanceOffset, x, _method.Body.GetILProcessor())
                        : new ArrayElementLoadable(arguments.Variable, i, x, _method.Body.GetILProcessor(), _creator))
                    .ToArray();

                callSourceMethod = _creator.CallMethodWithReturn(
                    _clonedMethod,
                    thisVariable == null ? null : new VariablePersistable(thisVariable),
                    returnValue == null ? null : new VariablePersistable(returnValue),
                    args);
            }
            else
            {
                callSourceMethod = _creator.CallMethodWithLocalParameters(
                    _method,
                    _clonedMethod,
                    thisVariable == null ? null : new VariablePersistable(thisVariable),
                    returnValue == null ? null : new VariablePersistable(returnValue));
            }
            
            if (allowChangingInputArguments)
            {
                // write byref variables back for origin source method:
                // the called method has written ref/out values into the local variables passed by address
                var copyBackBlock = CreateCopyBackByRefArguments(parameter =>
                {
                    var byRefVariable = ((ArrayElementLoadable)args[parameter.Index]).ByRefVariable;
                    return new[] { _ilProcessor.Create(OpCodes.Ldloc, byRefVariable) };
                });
                if (copyBackBlock != null)
                    callSourceMethod.Add(copyBackBlock);

                // update ExecutionArgs.Arguments with the ref/out values so OnExit sees them
                var updateArgumentsBlock = CreateUpdateArgumentsArrayFromByRefVariables(arguments, args);
                if (updateArgumentsBlock != null)
                    callSourceMethod.Add(updateArgumentsBlock);
            }

            callSourceMethod.Append(_ilProcessor);
            instructionCallStart = callSourceMethod.First;
            instructionCallEnd = callSourceMethod.Last;
        }

        private InstructionBlock CreateUpdateArgumentsArrayFromByRefVariables(NamedInstructionBlockChain arguments, ILoadable[] args)
        {
            var instructions = new List<Instruction>();
            foreach (var parameter in GetByRefParametersPassedViaArguments())
            {
                var byRefVariable = ((ArrayElementLoadable)args[parameter.Index]).ByRefVariable;
                instructions.Add(_ilProcessor.Create(OpCodes.Ldloc, arguments.Variable));
                instructions.Add(_ilProcessor.Create(OpCodes.Ldc_I4, parameter.Index));
                instructions.Add(_ilProcessor.Create(OpCodes.Ldloc, byRefVariable));

                var elementType = ((ByReferenceType)parameter.ParameterType.WithoutModifiers()).ElementType;
                // Resolve() returns the element type of an array
                if (elementType.IsGenericParameter || (!elementType.IsArray && (elementType.IsValueType || elementType.Resolve()?.IsValueType == true)))
                    instructions.Add(_ilProcessor.Create(OpCodes.Box, elementType));

                instructions.Add(_ilProcessor.Create(OpCodes.Stelem_Ref));
            }

            return instructions.Any()
                ? new InstructionBlock("Update arguments with ref values", instructions)
                : null;
        }

        private InstructionBlock CreateCopyBackByRefArgumentsFromArray(NamedInstructionBlockChain arguments)
        {
            return CreateCopyBackByRefArguments(parameter =>
            {
                var arg = new ArrayElementLoadable(arguments.Variable, parameter.Index, parameter, _ilProcessor, _creator);
                return arg.Load(false, true).Instructions;
            });
        }

        private InstructionBlock CreateCopyBackByRefArguments(Func<ParameterDefinition, IEnumerable<Instruction>> loadValue)
        {
            var copyBackInstructions = new List<Instruction>();
            foreach (var parameter in GetByRefParametersPassedViaArguments())
            {
                copyBackInstructions.Add(_ilProcessor.Create(OpCodes.Ldarg, parameter));
                copyBackInstructions.AddRange(loadValue(parameter));

                var elementType = ((ByReferenceType)parameter.ParameterType.WithoutModifiers()).ElementType;
                copyBackInstructions.Add(elementType.GetStIndInstruction());
            }

            return copyBackInstructions.Any()
                ? new InstructionBlock("Copy back ref values", copyBackInstructions)
                : null;
        }

        // ref/out parameters except ref structs and pointers, which cannot be boxed into ExecutionArgs.Arguments
        // and are passed directly to the called method instead, and except read only parameters
        private IEnumerable<ParameterDefinition> GetByRefParametersPassedViaArguments()
        {
            return _method.Parameters.Where(x => x.ParameterType.WithoutModifiers().IsByReference
                                                 && !x.ParameterType.CannotBeBoxed()
                                                 && !IsReadOnly(x));
        }

        // in and ref readonly parameters: a changed argument is passed to the method, but not written back to the
        // caller, whose variable (e.g. a readonly field) is read only for the method
        private static bool IsReadOnly(ParameterDefinition parameter)
        {
            return (parameter.IsIn && !parameter.IsOut)
                   || parameter.CustomAttributes.Any(a =>
                       a.AttributeType.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute"
                       || a.AttributeType.FullName == "System.Runtime.CompilerServices.RequiresLocationAttribute");
        }

        private Instruction WeaveOnExit(bool hasReturnValue, NamedInstructionBlockChain returnValue)
        {
            var onExitAspects = _aspects
                            .Where(x => x.AspectMethods.HasFlag(AspectMethods.OnExit))
                            .Reverse()
                            .ToList();

            Instruction instructionAfterCall = null;
            // ref structs and pointers cannot be boxed, ReturnValue stays null
            if (hasReturnValue && onExitAspects.Any() && ProvidesReturnValue && !_method.ReturnType.CannotBeBoxed())
            {
                var loadReturnValue = _creator.LoadValueOnStack(returnValue);

                var setMethodExecutionArgsReturnValue = _creator.SetMethodExecutionArgsReturnValue(
                    ExecutionArgs,
                    loadReturnValue);
                AddToEnd(setMethodExecutionArgsReturnValue);

                instructionAfterCall = setMethodExecutionArgsReturnValue.First;
            }

            foreach (var aspect in onExitAspects)
            {
                if (HasMultipleAspects)
                    AddToEnd(_creator.LoadMethodExecutionArgsTagFromPersistable(ExecutionArgs, aspect.TagPersistable));
                
                AddToEnd(_creator.CallAspectOnExit(aspect, ExecutionArgs));
            }

            if (hasReturnValue && onExitAspects.Any() && ReadsBackReturnValue)
                _creator.ReadReturnValue(ExecutionArgs, returnValue.Variable).Append(_ilProcessor);

            return instructionAfterCall;
        }

        private void HandleReturnValue(bool hasReturnValue, NamedInstructionBlockChain returnValue)
        {
            if (hasReturnValue)
                AddToEnd(_creator.LoadValueOnStack(returnValue));
            
            AddToEnd(_creator.CreateReturn());
        }

        protected virtual void WeaveOnException(IList<AspectData> allAspects, Instruction instructionCallStart, Instruction instructionCallEnd, Instruction instructionAfterCall, NamedInstructionBlockChain returnValue)
        {
            var realInstructionAfterCall = instructionAfterCall ?? instructionCallEnd.Next;
            var tryLeaveInstruction = Instruction.Create(OpCodes.Leave, realInstructionAfterCall);
            _ilProcessor.InsertAfter(instructionCallEnd, tryLeaveInstruction);

            var exception = _creator.SaveThrownException();
            var exceptionHandlerCurrent = exception.InsertAfter(tryLeaveInstruction, _ilProcessor);

            var pushException = _creator.LoadValueOnStack(exception);
            exceptionHandlerCurrent = pushException.InsertAfter(exceptionHandlerCurrent, _ilProcessor);

            var setExceptionFromStack =
                _creator.SetMethodExecutionArgsExceptionFromStack(ExecutionArgs);
            exceptionHandlerCurrent = setExceptionFromStack.InsertAfter(exceptionHandlerCurrent, _ilProcessor);
            
            var returnsByRef = _method.ReturnType.WithoutModifiers().IsByReference;
            var returnAfterHandling = new InstructionBlockChain();
            if (returnsByRef)
            {
                returnAfterHandling.Add(CreateThrowNotSupported(
                    "Swallowing an exception of a method with a ref return is not supported, the original exception is the inner exception.",
                    exception.Variable));
            }
            else
            {
                if (returnValue != null)
                    returnAfterHandling.Add(_creator.LoadValueOnStack(returnValue));
                returnAfterHandling.Add(new InstructionBlock("Return", Instruction.Create(OpCodes.Ret)));
            }

            var indices = allAspects
                .Select((asp, index) => new { asp, index })
                .Where(x => (x.asp.AspectMethods & AspectMethods.OnException) != 0)
                .Select(x => x.index)
                .Reverse()
                .ToList();
            foreach (var index in indices)
            {
                var onExceptionAspect = allAspects[index];
                if (HasMultipleAspects)
                {
                    var load = _creator.LoadMethodExecutionArgsTagFromPersistable(ExecutionArgs,
                        onExceptionAspect.TagPersistable);
                    exceptionHandlerCurrent = load.InsertAfter(exceptionHandlerCurrent, _ilProcessor);
                }

                var callAspectOnException =
                    _creator.CallAspectOnException(onExceptionAspect, ExecutionArgs);
                var nop = new InstructionBlock("Nop", Instruction.Create(OpCodes.Nop));

                var callOnExitsAndReturn = new InstructionBlockChain();
                for (var i = index-1; i >= 0; --i)
                {
                    var jthAspect = allAspects[i];
                    if ((jthAspect.AspectMethods & AspectMethods.OnExit) == 0) 
                        continue;
                    if (HasMultipleAspects)
                        callOnExitsAndReturn.Add(_creator.LoadMethodExecutionArgsTagFromPersistable(ExecutionArgs, jthAspect.TagPersistable));
                    callOnExitsAndReturn.Add(_creator.CallAspectOnExit(jthAspect, ExecutionArgs));
                }

                if (returnValue != null && !returnsByRef)
                {
                    if (ReadsBackReturnValue)
                        callOnExitsAndReturn.Add(_creator.ReadReturnValue(ExecutionArgs, returnValue.Variable));
                    callOnExitsAndReturn.Add(CreateReturnCompletedTaskIfNull(returnValue.Variable));
                }

                // long form: the handlers of the remaining aspects lie between here and the target,
                // Optimize() shortens the branch if it fits
                callOnExitsAndReturn.Add(new InstructionBlock("Leave", Instruction.Create(OpCodes.Leave, returnAfterHandling.First)));

                callAspectOnException.Add(_creator.IfFlowBehaviorIsAnyOf(ExecutionArgs, nop.First, callOnExitsAndReturn, 1, 3));

                callAspectOnException.Add(nop);
                callAspectOnException.InsertAfter(exceptionHandlerCurrent, _ilProcessor);
                exceptionHandlerCurrent = callAspectOnException.Last;
            }
            
            var flowBehaviorHandler = new InstructionBlockChain();
            flowBehaviorHandler.Add(new InstructionBlock("throw", Instruction.Create(OpCodes.Rethrow)));
            flowBehaviorHandler.InsertAfter(exceptionHandlerCurrent, _ilProcessor);

            returnAfterHandling.InsertAfter(flowBehaviorHandler.Last, _ilProcessor);

            _method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                CatchType = _creator.GetExceptionTypeReference(),
                TryStart = instructionCallStart,
                TryEnd = tryLeaveInstruction.Next,
                HandlerStart = tryLeaveInstruction.Next,
                HandlerEnd = flowBehaviorHandler.Last.Next
            });
        }

        private void Optimize()
        {
            // add DebuggerStepThrough attribute to avoid compiler searching for
            // non existing source code for weaved il code
            var attributeCtor = _creator.GetDebuggerStepThroughAttributeCtorReference();

            // Only add DebuggerStepThrough if not already present.
            if (_method.CustomAttributes.All(a => a.AttributeType.FullName != attributeCtor.DeclaringType.FullName))
                _method.CustomAttributes.Add(new CustomAttribute(attributeCtor));

            _method.Body.InitLocals = true;
            _method.Body.Optimize();
            Catel.Fody.CecilExtensions.UpdateDebugInfo(_method);
        }

        protected virtual void Finish()
        {
            _clonedMethod.Body.Optimize();
        }
        
        private void AddToEnd(InstructionBlockChain chain)
        {
            chain.Append(_ilProcessor);
        }
    }
}
