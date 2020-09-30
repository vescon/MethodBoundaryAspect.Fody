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

        protected bool HasMultipleAspects => _aspects.Count > 1;
        protected IPersistable ExecutionArgs { get; set; }

        public int WeaveCounter { get; private set; }

        public MethodWeaver(ModuleDefinition module, MethodDefinition method, IList<AspectData> aspects)
        {
            _module = module;
            _method = method;
            _creator = new InstructionBlockChainCreator(method, module);
            _ilProcessor = _method.Body.GetILProcessor();
            _aspects = aspects;
        }

        public void Weave()
        {
            if (_aspects.Count == 0)
                return;

            Setup();

            var arguments = _creator.CreateMethodArgumentsArray();
            AddToSetup(arguments);

            WeaveMethodExecutionArgs(arguments);

            SetupAspects();

            var hasReturnValue = !InstructionBlockCreator.IsVoid(_method.ReturnType);
            var returnValue = hasReturnValue
                    ? _creator.CreateVariable(_method.ReturnType)
                    : null;

            WeaveOnEntry(returnValue);
            
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
                    var clonedParameter = new GenericParameter(parameter.Name, clonedMethod);
                    if (parameter.HasConstraints)
                    {
                        foreach (var parameterConstraint in parameter.Constraints)
                        {
                            clonedParameter.Attributes = parameter.Attributes;
                            clonedParameter.Constraints.Add(parameterConstraint);
                        }
                    }

                    if (parameter.HasReferenceTypeConstraint)
                    {
                        clonedParameter.Attributes |= GenericParameterAttributes.ReferenceTypeConstraint;
                        clonedParameter.HasReferenceTypeConstraint = true;
                    }

                    if (parameter.HasNotNullableValueTypeConstraint)
                    {
                        clonedParameter.Attributes |= GenericParameterAttributes.NotNullableValueTypeConstraint;
                        clonedParameter.HasNotNullableValueTypeConstraint = true;
                    }

                    if (parameter.HasDefaultConstructorConstraint)
                    {
                        clonedParameter.Attributes |= GenericParameterAttributes.DefaultConstructorConstraint;
                        clonedParameter.HasDefaultConstructorConstraint = true;
                    }

                    clonedMethod.GenericParameters.Add(clonedParameter);
                }
            }

            if (method.DebugInformation.HasSequencePoints)
            {
                foreach (var sequencePoint in method.DebugInformation.SequencePoints)
                    clonedMethod.DebugInformation.SequencePoints.Add(sequencePoint);
            }

            clonedMethod.DebugInformation.Scope = new ScopeDebugInformation(method.Body.Instructions.First(), method.Body.Instructions.Last());
            return clonedMethod;
        }

        private static void ClearMethod(MethodDefinition method)
        {
            var body = method.Body;
            body.Variables.Clear();
            body.Instructions.Clear();
            body.ExceptionHandlers.Clear();
        }

        protected virtual void AddToSetup(InstructionBlockChain chain)
        {
            chain.Append(_ilProcessor);
        }

        protected virtual void WeaveMethodExecutionArgs(NamedInstructionBlockChain arguments)
        {
            var executionArgs = _creator.CreateMethodExecutionArgsInstance(
                arguments,
                _aspects[0].Info.AspectAttribute.AttributeType);
            AddToSetup(executionArgs);
            ExecutionArgs = executionArgs;
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

        private void WeaveOnEntry(IPersistable returnValue)
        {
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

                if (_method.ReturnType.IsByReference)
                {
                    var notSupportedExceptionCtorString =
                        _module.ImportReference(
                            _creator.GetExceptionTypeReference<NotSupportedException>()
                                .Resolve().Methods
                                .FirstOrDefault(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String"));
                    onExitChain.Add(new InstructionBlock("Throw NotSupported",
                        Instruction.Create(OpCodes.Ldstr, "Weaving early return from a method with a byref return type is not supported."),
                        Instruction.Create(OpCodes.Newobj, notSupportedExceptionCtorString),
                        Instruction.Create(OpCodes.Throw)));
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
                }

                if (returnValue != null)
                {
                    onExitChain.Add(_creator.ReadReturnValue(ExecutionArgs, returnValue));
                    onExitChain.Add(returnValue.Load(false, false));
                }

                onExitChain.Add(new InstructionBlock("Return", Instruction.Create(OpCodes.Ret)));

                flowChain.Add(_creator.IfFlowBehaviorIsAnyOf(ExecutionArgs, nopChain.First, onExitChain, 3));

                flowChain.Add(nopChain);
                AddToSetup(flowChain);
            }
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
            var allowChangingInputArguments = _aspects.Any(x => x.Info.AllowChangingInputArguments);
            if (allowChangingInputArguments)
            {
                // get arguments from ExecutionArgs because they could have been changed in aspect code
                args = _method.Parameters
                    .Select((x, i) => new ArrayElementLoadable(arguments.Variable, i, x, _method.Body.GetILProcessor(), _creator))
                    .Cast<ILoadable>()
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
                // write byref variables back for origin source method
                var copyBackInstructions = new List<Instruction>();
                foreach (var parameter in _method.Parameters.Where(x => x.ParameterType.IsByReference))
                {
                    var arg = args[parameter.Index];
                    copyBackInstructions.Add(_ilProcessor.Create(OpCodes.Ldarg, parameter));

                    var loadBlock = arg.Load(false, true);
                    copyBackInstructions.AddRange(loadBlock.Instructions);

                    var storeOpCode = parameter.ParameterType.MetadataType.GetStIndCode();
                    copyBackInstructions.Add(_ilProcessor.Create(storeOpCode));
                }

                if (copyBackInstructions.Any())
                {
                    var copyBackBlock = new InstructionBlock("Copy back ref values", copyBackInstructions);
                    callSourceMethod.Add(copyBackBlock);
                }
            }

            callSourceMethod.Append(_ilProcessor);
            instructionCallStart = callSourceMethod.First;
            instructionCallEnd = callSourceMethod.Last;
        }

        private Instruction WeaveOnExit(bool hasReturnValue, NamedInstructionBlockChain returnValue)
        {
            var onExitAspects = _aspects
                            .Where(x => x.AspectMethods.HasFlag(AspectMethods.OnExit))
                            .Reverse()
                            .ToList();

            Instruction instructionAfterCall = null;
            if (hasReturnValue && onExitAspects.Any())
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

            if (hasReturnValue && onExitAspects.Any())
                _creator.ReadReturnValue(ExecutionArgs, returnValue).Append(_ilProcessor);

            return instructionAfterCall;
        }

        private void HandleReturnValue(bool hasReturnValue, NamedInstructionBlockChain returnValue)
        {
            if (hasReturnValue)
                AddToEnd(_creator.LoadValueOnStack(returnValue));
            
            AddToEnd(_creator.CreateReturn());
        }

        protected virtual void WeaveOnException(IList<AspectData> allAspects, Instruction instructionCallStart, Instruction instructionCallEnd, Instruction instructionAfterCall, IPersistable returnValue)
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
            
            var returnAfterHandling = new InstructionBlockChain();
            if (returnValue != null)
                returnAfterHandling.Add(returnValue.Load(false, false));
            returnAfterHandling.Add(new InstructionBlock("Return", Instruction.Create(OpCodes.Ret)));

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

                if (returnValue != null)
                    callOnExitsAndReturn.Add(_creator.ReadReturnValue(ExecutionArgs, returnValue));

                callOnExitsAndReturn.Add(new InstructionBlock("Leave", Instruction.Create(OpCodes.Leave_S, returnAfterHandling.First)));

                callAspectOnException.Add(_creator.IfFlowBehaviorIsAnyOf(ExecutionArgs, nop.First, callOnExitsAndReturn, 1, 3));

                callAspectOnException.Add(nop);
                callAspectOnException.InsertAfter(exceptionHandlerCurrent, _ilProcessor);
                exceptionHandlerCurrent = callAspectOnException.Last;
            }
            
            var flowBehaviorHandler = new InstructionBlockChain();
            flowBehaviorHandler.Add(new InstructionBlock("throw", Instruction.Create(OpCodes.Rethrow)));
            flowBehaviorHandler.Add(new InstructionBlock("Leave", Instruction.Create(OpCodes.Leave_S, realInstructionAfterCall)));
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
            _clonedMethod.Body.InitLocals = true;
            _clonedMethod.Body.Optimize();
            Catel.Fody.CecilExtensions.UpdateDebugInfo(_clonedMethod);
        }
        
        private void AddToEnd(InstructionBlockChain chain)
        {
            chain.Append(_ilProcessor);
        }
    }
}
