using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
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

        protected bool HasMultipleAspects { get => _aspects.Count > 1; }
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

            WeaveOnEntry();

            var hasReturnValue = !InstructionBlockCreator.IsVoid(_method.ReturnType);
            var returnValue = hasReturnValue
                    ? _creator.CreateVariable(_method.ReturnType)
                    : null;

            HandleBody(returnValue?.Variable, out Instruction instructionCallStart, out Instruction instructionCallEnd);

            Instruction instructionAfterCall = WeaveOnExit(hasReturnValue, returnValue);

            HandleReturnValue(hasReturnValue, returnValue);

            var onExceptionAspects = _aspects
                .Where(x => x.AspectMethods.HasFlag(AspectMethods.OnException))
                .Reverse()
                .ToList();

            if (onExceptionAspects.Count != 0)
                WeaveOnException(onExceptionAspects, instructionCallStart, instructionCallEnd, instructionAfterCall);

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
                    var clonedparameter = new GenericParameter(parameter.Name, clonedMethod);
                    if (parameter.HasConstraints)
                    {
                        foreach (var parameterConstraint in parameter.Constraints)
                        {
                            clonedparameter.Attributes = parameter.Attributes;
                            clonedparameter.Constraints.Add(parameterConstraint);
                        }
                    }

                    if (parameter.HasReferenceTypeConstraint)
                    {
                        clonedparameter.Attributes |= GenericParameterAttributes.ReferenceTypeConstraint;
                        clonedparameter.HasReferenceTypeConstraint = true;
                    }

                    if (parameter.HasNotNullableValueTypeConstraint)
                    {
                        clonedparameter.Attributes |= GenericParameterAttributes.NotNullableValueTypeConstraint;
                        clonedparameter.HasNotNullableValueTypeConstraint = true;
                    }

                    if (parameter.HasDefaultConstructorConstraint)
                    {
                        clonedparameter.Attributes |= GenericParameterAttributes.DefaultConstructorConstraint;
                        clonedparameter.HasDefaultConstructorConstraint = true;
                    }

                    clonedMethod.GenericParameters.Add(clonedparameter);
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

        private void WeaveOnEntry()
        {
            foreach (var aspect in _aspects.Where(x => x.AspectMethods.HasFlag(AspectMethods.OnEntry)))
            {
                var call = _creator.CallAspectOnEntry(aspect, ExecutionArgs);
                AddToSetup(call);

                if (HasMultipleAspects)
                    AddToSetup(_creator.SaveMethodExecutionArgsTagToPersistable(ExecutionArgs, aspect.TagPersistable));
            }
        }
        
        protected virtual void HandleBody(VariableDefinition returnValue, out Instruction instructionCallStart, out Instruction instructionCallEnd)
        {
            VariableDefinition thisVariable = null;
            if (!_method.IsStatic)
            {
                var thisVariableBlock = _creator.CreateThisVariable(_method.DeclaringType);
                thisVariableBlock.Append(_ilProcessor);
                thisVariable = thisVariableBlock.Variable;
            }

            var callSourceMethod = _creator.CallMethodWithLocalParameters(
                _method,
                _clonedMethod,
                thisVariable == null ? null : new VariablePersistable(thisVariable),
                returnValue == null ? null : new VariablePersistable(returnValue));
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

        protected virtual void WeaveOnException(List<AspectData> onExceptionAspects, Instruction instructionCallStart, Instruction instructionCallEnd, Instruction instructionAfterCall)
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

            foreach (var onExceptionAspect in onExceptionAspects)
            {
                if (HasMultipleAspects)
                {
                    var load = _creator.LoadMethodExecutionArgsTagFromPersistable(ExecutionArgs,
                        onExceptionAspect.TagPersistable);
                    exceptionHandlerCurrent = load.InsertAfter(exceptionHandlerCurrent, _ilProcessor);
                }

                var callAspectOnException =
                    _creator.CallAspectOnException(onExceptionAspect, ExecutionArgs);
                callAspectOnException.InsertAfter(exceptionHandlerCurrent, _ilProcessor);
                exceptionHandlerCurrent = callAspectOnException.Last;
            }

            var pushException2 = _creator.LoadValueOnStack(exception);
            exceptionHandlerCurrent = pushException2.InsertAfter(exceptionHandlerCurrent, _ilProcessor);

            ////var catchLeaveInstruction = Instruction.Create(OpCodes.Leave, realInstructionAfterCall);
            var catchLastInstruction = Instruction.Create(OpCodes.Throw);
            _ilProcessor.InsertAfter(exceptionHandlerCurrent, catchLastInstruction);

            _method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                CatchType = _creator.GetExceptionTypeReference(),
                TryStart = instructionCallStart,
                TryEnd = tryLeaveInstruction.Next,
                HandlerStart = tryLeaveInstruction.Next,
                HandlerEnd = catchLastInstruction.Next
            });
        }

        private void Optimize()
        {
            // add DebuggerStepThrough attribute to avoid compiler searching for
            // non existing soure code for weaved il code
            var attributeCtor = _creator.GetDebuggerStepThroughAttributeCtorReference();

            // Only add DebuggerStepThrough if not already present.
            if (!_method.CustomAttributes.Any(a => a.AttributeType.FullName == attributeCtor.DeclaringType.FullName))
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
