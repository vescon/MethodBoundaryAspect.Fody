using System;
using System.Collections.Generic;
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

            var blockChain = new NamedInstructionBlockChain(argumentsArrayVariable,
                argumentsTypeReference);
            blockChain.Add(createObjectArrayWithMethodArgumentsBlock);
            return blockChain;
        }

        public NamedInstructionBlockChain CreateThisVariable()
        {
            var type = _method.DeclaringType;
            TypeReference typeRef = type;
            if (typeRef.IsValueType)
                typeRef = new ByReferenceType(type);
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
            TypeReference anyAspectTypeDefinition)
        {
            // instance value
            var createThisVariableBlock = CreateThisVariable();

            // MethodExecutionArgs instance
            var onEntryMethodTypeRef =
                anyAspectTypeDefinition.Resolve().BaseType.Resolve().Methods.Single(x => x.Name == "OnEntry");
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
            var methodBaseGetCurrentMethod = _referenceFinder.GetMethodReference(methodBaseTypeRef,
                md => md.Name == "GetCurrentMethod");
            var callGetCurrentMethodBlock = _creator.CallStaticMethod(methodBaseGetCurrentMethod, new VariablePersistable(methodBaseVariable));

            var methodExecutionArgsSetMethodBaseMethodRef =
                _referenceFinder.GetMethodReference(methodExecutionArgsTypeRef, md => md.Name == "set_Method");
            var callSetMethodBlock = _creator.CallVoidInstanceMethod(methodExecutionArgsSetMethodBaseMethodRef,
                new VariablePersistable(methodExecutionArgsVariable),
                new VariablePersistable(methodBaseVariable));

            var newMethodExectionArgsBlockChain = new NamedInstructionBlockChain(methodExecutionArgsVariable,
                methodExecutionArgsTypeRef);
            newMethodExectionArgsBlockChain.Add(newObjectMethodExecutionArgsBlock);
            newMethodExectionArgsBlockChain.Add(callSetArgumentsBlock);
            newMethodExectionArgsBlockChain.Add(callGetCurrentMethodBlock);
            newMethodExectionArgsBlockChain.Add(callSetMethodBlock);
            if (callSetInstanceBlock != null)
            {
                newMethodExectionArgsBlockChain.Add(createThisVariableBlock);
                newMethodExectionArgsBlockChain.Add(callSetInstanceBlock);
            }

            return newMethodExectionArgsBlockChain;
        }

        public NamedInstructionBlockChain CreateAspectInstance(CustomAttribute aspect)
        {
            var aspectTypeReference = _moduleDefinition.ImportReference(aspect.AttributeType);
            var aspectVariable = _creator.CreateVariable(aspectTypeReference);
            var newObjectAspectBlock = _creator.NewObject(aspectVariable, aspectTypeReference, _moduleDefinition, aspect);

            var newObjectAspectBlockChain = new NamedInstructionBlockChain(aspectVariable, aspectTypeReference);
            newObjectAspectBlockChain.Add(newObjectAspectBlock);
            return newObjectAspectBlockChain;
        }

        public InstructionBlockChain SetMethodExecutionArgsReturnValue(
            IPersistable newMethodExectionArgsBlockChain, NamedInstructionBlockChain loadReturnValue)
        {
            var methodExecutionArgsSetReturnValueMethodRef =
                _referenceFinder.GetMethodReference(newMethodExectionArgsBlockChain.PersistedType,
                    md => md.Name == "set_ReturnValue");
            var callSetReturnValueBlock = _creator.CallVoidInstanceMethod(methodExecutionArgsSetReturnValueMethodRef,
                newMethodExectionArgsBlockChain,
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

        public NamedInstructionBlockChain SaveReturnValue()
        {
            var returnValueVariable = _creator.CreateVariable(_method.ReturnType);
            var block = new NamedInstructionBlockChain(returnValueVariable, _method.ReturnType);

            var instructions = _creator.SaveReturnValueFromStack(returnValueVariable);
            block.Add(instructions);
            return block;
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

        public InstructionBlockChain CallAspectOnEntry(AspectData aspectInstance,
            IPersistable executionArgs)
        {
            var onEntryMethodRef = _referenceFinder.GetMethodReference(aspectInstance.Info.AspectAttribute.AttributeType,
                md => md.Name == "OnEntry");
            var callOnEntryBlock = _creator.CallVoidInstanceMethod(onEntryMethodRef,
                aspectInstance.AspectPersistable, executionArgs);

            var callAspectOnEntryBlockChain = new InstructionBlockChain();
            callAspectOnEntryBlockChain.Add(callOnEntryBlock);
            return callAspectOnEntryBlockChain;
        }

        public InstructionBlockChain CallAspectOnExit(AspectData aspectData,
            IPersistable executionArgs)
        {
            var onExitMethodRef = _referenceFinder.GetMethodReference(aspectData.Info.AspectAttribute.AttributeType,
                md => md.Name == "OnExit");
            var callOnExitBlock = _creator.CallVoidInstanceMethod(onExitMethodRef,
                aspectData.AspectPersistable, executionArgs);

            var callAspectOnExitBlockChain = new InstructionBlockChain();
            callAspectOnExitBlockChain.Add(callOnExitBlock);
            return callAspectOnExitBlockChain;
        }

        public InstructionBlockChain CallAspectOnException(AspectData aspectData,
            IPersistable executionArgs)
        {
            var onExceptionMethodRef = _referenceFinder.GetMethodReference(aspectData.Info.AspectAttribute.AttributeType,
                md => md.Name == "OnException");
            var callOnExceptionBlock = _creator.CallVoidInstanceMethod(onExceptionMethodRef,
                aspectData.AspectPersistable, executionArgs);

            var callAspectOnExceptionBlockChain = new InstructionBlockChain();
            callAspectOnExceptionBlockChain.Add(callOnExceptionBlock);
            return callAspectOnExceptionBlockChain;
        }

        public InstructionBlockChain CallMethodWithLocalParameters(MethodDefinition method,
            MethodDefinition targetMethod, ILoadable instance, IPersistable resultVariable)
        {
            int instanceOffset = (method.IsStatic ? 0 : 1);
            ILoadable[] args = new ILoadable[method.Parameters.Count];

            for (int i = 0; i < args.Length; ++i)
                args[i] = new ArgumentLoadable(i + instanceOffset, method.Parameters[i], _method.Body.GetILProcessor());

            return CallMethodWithReturn(targetMethod, instance, resultVariable, args);
        }

        public InstructionBlockChain CallMethodWithReturn(MethodReference method,
            ILoadable instance,
            IPersistable returnValue,
            params ILoadable[] arguments)
        {
            var type = FixTypeReference(method.DeclaringType);
            method = FixMethodReference(type, method);

            InstructionBlock block;
            
            if (method.Resolve().IsStatic)
                block = _creator.CallStaticMethod(method, returnValue, arguments);
            else
                block = _creator.CallInstanceMethod(method, instance, returnValue, arguments);

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