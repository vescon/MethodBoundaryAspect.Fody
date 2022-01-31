using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    public class InstructionBlockChainCreator
    {
        private readonly MethodDefinition _method;
        private readonly ModuleDefinition _moduleDefinition;

        private readonly ReferenceFinder _referenceFinder;
        private readonly InstructionBlockCreator _creator;

        public InstructionBlockChainCreator(
            MethodDefinition method, 
            ModuleDefinition moduleDefinition)
        {
            _method = method;
            _moduleDefinition = moduleDefinition;

            _referenceFinder = new ReferenceFinder(_moduleDefinition);
            _creator = new InstructionBlockCreator(_method, _referenceFinder);
        }
        
        public NamedInstructionBlockChain CreateAndNewUpAspect(CustomAttribute aspect)
        {
            var aspectTypeReference = _moduleDefinition.ImportReference(aspect.AttributeType);
            var aspectLocal = _creator.CreateVariable(aspectTypeReference);
            var newObjectAspectBlock = _creator.NewObject(aspectLocal, aspectTypeReference, _moduleDefinition, aspect);

            var newObjectAspectBlockChain = new NamedInstructionBlockChain(aspectLocal, aspectTypeReference);
            newObjectAspectBlockChain.Add(newObjectAspectBlock);
            return newObjectAspectBlockChain;
        }

        public NamedInstructionBlockChain CreateVariable(TypeReference typeReference)
        {
            var variable = _creator.CreateVariable(typeReference);
            return new NamedInstructionBlockChain(variable, typeReference);
        }

        public NamedInstructionBlockChain CreateObjectVariable()
        {
            var typeReference = _referenceFinder.GetTypeReference(typeof (object));
            var variable = _creator.CreateVariable(typeReference);
            _method.Body.Variables.Add(variable);
            return new NamedInstructionBlockChain(variable, typeReference);
        }

        public TypeReference GetExceptionTypeReference()
        {
            return _referenceFinder.GetTypeReference(typeof (Exception));
        }

        public TypeReference GetExceptionTypeReference<T>()
            where T : Exception
        {
            return _referenceFinder.GetTypeReference(typeof(T));
        }

        public MethodReference GetDebuggerStepThroughAttributeCtorReference()
        {
            var typeReference = _referenceFinder.GetTypeReference(typeof (System.Diagnostics.DebuggerStepThroughAttribute), "System.Diagnostics.Debug");
            return _referenceFinder.GetConstructorReference(typeReference, x => true);
        }

        public NamedInstructionBlockChain CreateMethodArgumentsArray()
        {
            //  argument values
            var argumentsTypeReference = _referenceFinder.GetTypeReference(typeof (object[]));
            var argumentsArrayVariable = _creator.CreateVariable(argumentsTypeReference);
            var createObjectArrayWithMethodArgumentsBlock =
                _creator.CreateObjectArrayWithMethodArguments(argumentsArrayVariable,
                    _referenceFinder.GetTypeReference(typeof (object)));

            var blockChain = new NamedInstructionBlockChain(argumentsArrayVariable, argumentsTypeReference);
            blockChain.Add(createObjectArrayWithMethodArgumentsBlock);
            return blockChain;
        }

        public NamedInstructionBlockChain CreateThisVariable()
        {
            var type = _method.DeclaringType;
            TypeReference typeRef = type;
            if (typeRef.IsValueType)
                typeRef = new ByReferenceType(type); // Unused? why?
            return CreateThisVariable(type);
        }

        public NamedInstructionBlockChain CreateThisVariable(TypeReference typeReference)
        {
            typeReference = FixTypeReference(typeReference);
            if (typeReference.IsValueType)
                typeReference = new ByReferenceType(typeReference);

            var instanceVariable = _creator.CreateVariable(typeReference);
            var block = _creator.CreateThisVariable(instanceVariable);
            var result = new NamedInstructionBlockChain(instanceVariable, typeReference);
            result.InstructionBlocks.Add(block);
            return result;
        }

        public NamedInstructionBlockChain CreateMethodExecutionArgsInstance(
            NamedInstructionBlockChain argumentsArrayChain,
            TypeReference anyAspectTypeDefinition,
            MethodDefinition method,
            MethodInfoCompileTimeWeaver methodInfoCompileTimeWeaver)
        {
            // instance value
            var createThisVariableBlock = CreateThisVariable();

            // MethodExecutionArgs instance
            var onEntryMethodTypeRef =
                anyAspectTypeDefinition.Resolve().BaseType.Resolve().Methods.Single(AspectMethodCriteria.IsOnEntryMethod);
            var firstParameterType = onEntryMethodTypeRef.Parameters.Single().ParameterType;
            var methodExecutionArgsTypeRef = _moduleDefinition.ImportReference(firstParameterType);
            
            var methodExecutionArgsVariable = _creator.CreateVariable(methodExecutionArgsTypeRef);
            var newObjectMethodExecutionArgsBlock = _creator.NewObject(
                methodExecutionArgsVariable,
                methodExecutionArgsTypeRef, 
                _moduleDefinition);

            InstructionBlock callSetInstanceBlock = null;
            if (!_method.IsStatic)
            {
                var methodExecutionArgsSetInstanceMethodRef =
                    _referenceFinder.GetMethodReference(methodExecutionArgsTypeRef, md => md.Name == "set_Instance");
                callSetInstanceBlock = _creator.CallVoidInstanceMethod(methodExecutionArgsSetInstanceMethodRef,
                    new VariablePersistable(methodExecutionArgsVariable),
                    new VariablePersistable(createThisVariableBlock.Variable));
            }

            var methodExecutionArgsSetArgumentsMethodRef =
                _referenceFinder.GetMethodReference(methodExecutionArgsTypeRef, md => md.Name == "set_Arguments");
            var callSetArgumentsBlock = _creator.CallVoidInstanceMethod(methodExecutionArgsSetArgumentsMethodRef,
                new VariablePersistable(methodExecutionArgsVariable),
                new VariablePersistable(argumentsArrayChain.Variable));

            var methodBaseTypeRef = _referenceFinder.GetTypeReference(typeof (MethodBase));
            var methodBaseVariable = _creator.CreateVariable(methodBaseTypeRef);
            InstructionBlock callGetCurrentMethodBlock;
            var variablePersistable = new VariablePersistable(methodBaseVariable);
            if (methodInfoCompileTimeWeaver?.IsEnabled != true)
            {
                // fallback: slow GetCurrentMethod
                var methodBaseGetCurrentMethod = _referenceFinder.GetMethodReference(methodBaseTypeRef,
                    md => md.Name == "GetCurrentMethod");
                callGetCurrentMethodBlock = _creator.CallStaticMethod(methodBaseGetCurrentMethod, variablePersistable);
            }
            else
            {
                // fast: precompiled method info token
                methodInfoCompileTimeWeaver.AddMethod(method);
                callGetCurrentMethodBlock = methodInfoCompileTimeWeaver.PushMethodInfoOnStack(method, variablePersistable);
            }

            var methodExecutionArgsSetMethodBaseMethodRef =
                _referenceFinder.GetMethodReference(methodExecutionArgsTypeRef, md => md.Name == "set_Method");
            var callSetMethodBlock = _creator.CallVoidInstanceMethod(methodExecutionArgsSetMethodBaseMethodRef,
                new VariablePersistable(methodExecutionArgsVariable),
                variablePersistable);

            var newMethodExecutionArgsBlockChain = new NamedInstructionBlockChain(methodExecutionArgsVariable,
                methodExecutionArgsTypeRef);
            newMethodExecutionArgsBlockChain.Add(newObjectMethodExecutionArgsBlock);
            newMethodExecutionArgsBlockChain.Add(callSetArgumentsBlock);
            newMethodExecutionArgsBlockChain.Add(callGetCurrentMethodBlock);
            newMethodExecutionArgsBlockChain.Add(callSetMethodBlock);
            if (callSetInstanceBlock != null)
            {
                newMethodExecutionArgsBlockChain.Add(createThisVariableBlock);
                newMethodExecutionArgsBlockChain.Add(callSetInstanceBlock);
            }

            return newMethodExecutionArgsBlockChain;
        }
        
        public InstructionBlockChain SetMethodExecutionArgsReturnValue(
            IPersistable newMethodExecutionArgsBlockChain, NamedInstructionBlockChain loadReturnValue)
        {
            var methodExecutionArgsSetReturnValueMethodRef =
                _referenceFinder.GetMethodReference(newMethodExecutionArgsBlockChain.PersistedType,
                    md => md.Name == "set_ReturnValue");
            var callSetReturnValueBlock = _creator.CallVoidInstanceMethod(methodExecutionArgsSetReturnValueMethodRef,
                newMethodExecutionArgsBlockChain,
                new VariablePersistable(loadReturnValue.Variable));

            var block = new InstructionBlockChain();
            block.Add(callSetReturnValueBlock);
            return block;
        }

        public NamedInstructionBlockChain SetMethodExecutionArgsExceptionFromStack(
            IPersistable createMethodExecutionArgsInstance)
        {
            var exceptionTypeRef = _referenceFinder.GetTypeReference(typeof (Exception));
            var exceptionVariable = _creator.CreateVariable(exceptionTypeRef);
            var assignExceptionVariable = _creator.AssignValueFromStack(exceptionVariable);

            var methodExecutionArgsSetExceptionMethodRef =
                _referenceFinder.GetMethodReference(createMethodExecutionArgsInstance.PersistedType,
                    md => md.Name == "set_Exception");
            var callSetExceptionBlock = _creator.CallVoidInstanceMethod(methodExecutionArgsSetExceptionMethodRef,
                createMethodExecutionArgsInstance,
                new VariablePersistable(exceptionVariable));

            var block = new NamedInstructionBlockChain(exceptionVariable, exceptionTypeRef);
            block.Add(assignExceptionVariable);
            block.Add(callSetExceptionBlock);
            return block;
        }

        public InstructionBlockChain SaveMethodExecutionArgsTagToPersistable(
            IPersistable createMethodExecutionArgsInstance,
            IPersistable tag)
        {
            var getMethod =
                _referenceFinder.GetMethodReference(createMethodExecutionArgsInstance.PersistedType,
                    md => md.Name == "get_MethodExecutionTag");
            var chain = new InstructionBlockChain();
            chain.Add(_creator.CallInstanceMethod(getMethod, createMethodExecutionArgsInstance, tag));
            return chain;
        }

        public InstructionBlockChain LoadMethodExecutionArgsTagFromPersistable(
            IPersistable createMethodExecutionArgsInstance,
            IPersistable tag)
        {
            var setMethod = _referenceFinder.GetMethodReference(createMethodExecutionArgsInstance.PersistedType,
                    md => md.Name == "set_MethodExecutionTag");
            var chain = new InstructionBlockChain();
            chain.Add(_creator.CallVoidInstanceMethod(setMethod, createMethodExecutionArgsInstance, tag));
            return chain;
        }

        public NamedInstructionBlockChain LoadValueOnStack(NamedInstructionBlockChain instructionBlock)
        {
            var block = new NamedInstructionBlockChain(instructionBlock.Variable, instructionBlock.TypeReference);
            if (instructionBlock.Variable == null)
                return block;

            var loadReturnValueBlock = _creator.PushValueOnStack(instructionBlock.Variable);
            block.Add(loadReturnValueBlock);
            return block;
        }

        public InstructionBlockChain ReadReturnValue(IPersistable executionArgs, IPersistable returnValue)
        {
            if (InstructionBlockCreator.IsVoid(_method.ReturnType))
                return new InstructionBlockChain();

            var getReturnValue = _referenceFinder.GetMethodReference(executionArgs.PersistedType, md => md.Name == "get_ReturnValue");
            var readValueBlock = _creator.CallInstanceMethod(getReturnValue, executionArgs, returnValue);

            var readValueBlockChain = new InstructionBlockChain();
            readValueBlockChain.Add(readValueBlock);
            return readValueBlockChain;
        } 
        
        public NamedInstructionBlockChain SaveThrownException()
        {
            var exceptionTypeRef = _referenceFinder.GetTypeReference(typeof(Exception));
            var exceptionVariable = _creator.CreateVariable(exceptionTypeRef);
            var block = new NamedInstructionBlockChain(exceptionVariable, exceptionTypeRef);

            var instructions = _creator.AssignValueFromStack(exceptionVariable);
            block.Add(instructions);
            return block;
        }

        public InstructionBlockChain CallAspectOnEntry(
            AspectData aspectInstance,
            IPersistable executionArgs)
        {
            var onEntryMethodRef = _referenceFinder.GetMethodReference(
                aspectInstance.Info.AspectAttribute.AttributeType,
                AspectMethodCriteria.IsOnEntryMethod);
            var callOnEntryBlock = _creator.CallVoidInstanceMethod(onEntryMethodRef,
                aspectInstance.AspectPersistable, executionArgs);

            var callAspectOnEntryBlockChain = new InstructionBlockChain();
            callAspectOnEntryBlockChain.Add(callOnEntryBlock);
            return callAspectOnEntryBlockChain;
        }

        public InstructionBlockChain CallAspectOnExit(AspectData aspectData,
            IPersistable executionArgs)
        {
            var onExitMethodRef = _referenceFinder.GetMethodReference(
                aspectData.Info.AspectAttribute.AttributeType,
                AspectMethodCriteria.IsOnExitMethod);
            var callOnExitBlock = _creator.CallVoidInstanceMethod(onExitMethodRef,
                aspectData.AspectPersistable, executionArgs);

            var callAspectOnExitBlockChain = new InstructionBlockChain();
            callAspectOnExitBlockChain.Add(callOnExitBlock);
            return callAspectOnExitBlockChain;
        }

        public InstructionBlockChain CallAspectOnException(
            AspectData aspectData,
            IPersistable executionArgs)
        {
            var onExceptionMethodRef = _referenceFinder.GetMethodReference(
                aspectData.Info.AspectAttribute.AttributeType,
                AspectMethodCriteria.IsOnExceptionMethod);
            var callOnExceptionBlock = _creator.CallVoidInstanceMethod(onExceptionMethodRef,
                aspectData.AspectPersistable, executionArgs);

            var callAspectOnExceptionBlockChain = new InstructionBlockChain();
            callAspectOnExceptionBlockChain.Add(callOnExceptionBlock);
            return callAspectOnExceptionBlockChain;
        }

        public InstructionBlockChain CallMethodWithLocalParameters(
            MethodDefinition method,
            MethodDefinition targetMethod,
            ILoadable instance,
            IPersistable resultVariable)
        {
            var instanceOffset = method.IsStatic ? 0 : 1;
            var args = new ILoadable[method.Parameters.Count];

            for (var i = 0; i < args.Length; ++i)
                args[i] = new ArgumentLoadable(i + instanceOffset, method.Parameters[i], _method.Body.GetILProcessor());

            return CallMethodWithReturn(targetMethod, instance, resultVariable, args);
        }

        public InstructionBlockChain CallMethodWithReturn(
            MethodReference method,
            ILoadable instance,
            IPersistable returnValue,
            params ILoadable[] arguments)
        {
            var type = FixTypeReference(method.DeclaringType);
            method = FixMethodReference(type, method);

            var block = method.Resolve().IsStatic
                ? _creator.CallStaticMethod(method, returnValue, arguments)
                : _creator.CallInstanceMethod(method, instance, returnValue, arguments);

            var chain = new InstructionBlockChain();
            chain.Add(block);
            return chain;
        }

        public InstructionBlockChain CallVoidMethod(MethodReference method, ILoadable instance, params ILoadable[] arguments)
        {
            return CallMethodWithReturn(method, instance, null, arguments);
        }

        public InstructionBlockChain CreateReturn()
        {
            var chain = new InstructionBlockChain();
            chain.Add(new InstructionBlock("Return", _creator.CreateReturn()));
            return chain;
        }

        public InstructionBlockChain IfFlowBehaviorIsAnyOf(ILoadable args, Instruction nextInstruction, InstructionBlockChain thenBody, params int[] behaviors)
        {
            return IfFlowBehaviorIsAnyOf(_creator.CreateVariable, args, nextInstruction, thenBody, behaviors);
        }

        public InstructionBlockChain IfFlowBehaviorIsAnyOf(Func<TypeReference, VariableDefinition> variableFactory, ILoadable args, Instruction nextInstruction, InstructionBlockChain thenBody, params int[] behaviors)
        {
            var typeRef = args.PersistedType.Resolve();
            var getFlowBehavior = _referenceFinder.GetMethodReference(typeRef, m => m.Name == "get_FlowBehavior");
            var flowBehaviorLocal = new VariablePersistable(variableFactory(_moduleDefinition.ImportReference(getFlowBehavior.ReturnType)));
            var flowBehaviorHandler = CallMethodWithReturn(getFlowBehavior, args, flowBehaviorLocal);

            if (behaviors.Length == 0)
                return flowBehaviorHandler;
            for (var i = 0; i < behaviors.Length - 1; ++i)
            {
                flowBehaviorHandler.Add(flowBehaviorLocal.Load(false, false));
                flowBehaviorHandler.Add(new InstructionBlock("FlowBehavior", Instruction.Create(OpCodes.Ldc_I4, behaviors[i])));
                flowBehaviorHandler.Add(new InstructionBlock("If == then goto the 'then' block", Instruction.Create(OpCodes.Beq_S, thenBody.First)));
            }
            
            flowBehaviorHandler.Add(flowBehaviorLocal.Load(false, false));
            flowBehaviorHandler.Add(new InstructionBlock("FlowBehavior", Instruction.Create(OpCodes.Ldc_I4, behaviors[behaviors.Length - 1])));
            flowBehaviorHandler.Add(new InstructionBlock("If != then skip", Instruction.Create(OpCodes.Bne_Un, nextInstruction)));

            flowBehaviorHandler.Add(thenBody);

            return flowBehaviorHandler;
        }

        private static TypeReference FixTypeReference(TypeReference typeReference)
        {
            if (!typeReference.HasGenericParameters)
                return typeReference;

            // workaround for method in generic type
            // https://stackoverflow.com/questions/4968755/mono-cecil-call-generic-base-class-method-from-other-assembly
            var genericParameters = typeReference.GenericParameters
                .Select(x => x.GetElementType())
                .ToArray();

            return typeReference.MakeGenericType(genericParameters);
        }

        private static MethodReference FixMethodReference(TypeReference declaringType, MethodReference targetMethod)
        {
            // Taken and adapted from
            // https://stackoverflow.com/questions/4968755/mono-cecil-call-generic-base-class-method-from-other-assembly
            if (targetMethod is MethodDefinition)
            {
                var newTargetMethod = new MethodReference(targetMethod.Name, targetMethod.ReturnType, declaringType)
                {
                    HasThis = targetMethod.HasThis,
                    ExplicitThis = targetMethod.ExplicitThis,
                    CallingConvention = targetMethod.CallingConvention
                };
                foreach (var p in targetMethod.Parameters)
                    newTargetMethod.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, p.ParameterType));
                foreach (var gp in targetMethod.GenericParameters)
                    newTargetMethod.GenericParameters.Add(new GenericParameter(gp.Name, newTargetMethod));
                
                targetMethod = newTargetMethod;
            }
            else
                targetMethod.DeclaringType = declaringType;

            if (targetMethod.HasGenericParameters)
                return targetMethod.MakeGeneric();
            
            return targetMethod;
        }
    }
}