using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

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
        
        public NamedInstructionBlockChain CreateVariable(TypeReference typeReference)
        {
            var variable = _creator.CreateVariable(typeReference);
            return new NamedInstructionBlockChain(variable, typeReference);
        }

        public NamedInstructionBlockChain CreateObjectVariable()
        {
            var typeReference = _referenceFinder.GetTypeReference(typeof (object));
            var variable = _creator.CreateVariable(typeReference);
            return new NamedInstructionBlockChain(variable, typeReference);
        }

        public TypeReference GetExceptionTypeReference()
        {
            return _referenceFinder.GetTypeReference(typeof (Exception));
        }

        public MethodReference GetDebuggerStepThroughAttributeCtorReference()
        {
            var typeReference = _referenceFinder.GetTypeReference(typeof (System.Diagnostics.DebuggerStepThroughAttribute));
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
            var type = typeof(object);
            var objectType = _referenceFinder.GetTypeReference(type);
            return CreateThisVariable(objectType);
        }

        public NamedInstructionBlockChain CreateThisVariable(TypeReference typeReference)
        {
            typeReference = FixTypeReference(typeReference);

            var instanceVariable = _creator.CreateVariable(typeReference);
            var block = _creator.CreateThisVariable(instanceVariable, typeReference);
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
                callSetInstanceBlock = _creator.CallVoidInstanceMethod(methodExecutionArgsSetInstanceMethodRef, methodExecutionArgsVariable, createThisVariableBlock.Variable);
            }

            var methodExecutionArgsSetArgumentsMethodRef =
                _referenceFinder.GetMethodReference(methodExecutionArgsTypeRef, md => md.Name == "set_Arguments");
            var callSetArgumentsBlock = _creator.CallVoidInstanceMethod(methodExecutionArgsSetArgumentsMethodRef, methodExecutionArgsVariable, argumentsArrayChain.Variable);

            var methodBaseTypeRef = _referenceFinder.GetTypeReference(typeof (MethodBase));
            var methodBaseVariable = _creator.CreateVariable(methodBaseTypeRef);
            var methodBaseGetCurrentMethod = _referenceFinder.GetMethodReference(methodBaseTypeRef,
                md => md.Name == "GetCurrentMethod");
            var callGetCurrentMethodBlock = _creator.CallStaticMethod(methodBaseGetCurrentMethod, methodBaseVariable);

            var methodExecutionArgsSetMethodBaseMethodRef =
                _referenceFinder.GetMethodReference(methodExecutionArgsTypeRef, md => md.Name == "set_Method");
            var callSetMethodBlock = _creator.CallVoidInstanceMethod(methodExecutionArgsSetMethodBaseMethodRef, methodExecutionArgsVariable, methodBaseVariable);

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
            NamedInstructionBlockChain newMethodExectionArgsBlockChain, NamedInstructionBlockChain loadReturnValue)
        {
            var methodExecutionArgsSetReturnValueMethodRef =
                _referenceFinder.GetMethodReference(newMethodExectionArgsBlockChain.TypeReference,
                    md => md.Name == "set_ReturnValue");
            var callSetReturnValueBlock = _creator.CallVoidInstanceMethod(methodExecutionArgsSetReturnValueMethodRef, newMethodExectionArgsBlockChain.Variable, loadReturnValue.Variable);

            var block = new InstructionBlockChain();
            block.Add(callSetReturnValueBlock);
            return block;
        }

        public NamedInstructionBlockChain SetMethodExecutionArgsExceptionFromStack(
            NamedInstructionBlockChain createMethodExecutionArgsInstance)
        {
            var exceptionTypeRef = _referenceFinder.GetTypeReference(typeof (Exception));
            var exceptionVariable = _creator.CreateVariable(exceptionTypeRef);
            var assignExceptionVariable = _creator.AssignValueFromStack(exceptionVariable);

            var methodExecutionArgsSetExceptionMethodRef =
                _referenceFinder.GetMethodReference(createMethodExecutionArgsInstance.TypeReference,
                    md => md.Name == "set_Exception");
            var callSetExceptionBlock = _creator.CallVoidInstanceMethod(methodExecutionArgsSetExceptionMethodRef, createMethodExecutionArgsInstance.Variable, exceptionVariable);

            var block = new NamedInstructionBlockChain(exceptionVariable, exceptionTypeRef);
            block.Add(assignExceptionVariable);
            block.Add(callSetExceptionBlock);
            return block;
        }

        public NamedInstructionBlockChain SaveMethodExecutionArgsTagToVariable(
            NamedInstructionBlockChain createMethodExecutionArgsInstance,
            VariableDefinition variable)
        {
            var getMethod =
                _referenceFinder.GetMethodReference(createMethodExecutionArgsInstance.TypeReference,
                    md => md.Name == "get_MethodExecutionTag");

            var objectTypeRef = _referenceFinder.GetTypeReference(typeof(object));
            var call = _creator.CallInstanceMethod(getMethod, createMethodExecutionArgsInstance.Variable, variable);

            var block = new NamedInstructionBlockChain(variable, objectTypeRef);
            block.Add(call);
            return block;
        }

        public NamedInstructionBlockChain LoadMethodExecutionArgsTagFromVariable(
            NamedInstructionBlockChain createMethodExecutionArgsInstance,
            VariableDefinition value)
        {
            var objectTypeRef = _referenceFinder.GetTypeReference(typeof (object));

            var setMethod =
                _referenceFinder.GetMethodReference(createMethodExecutionArgsInstance.TypeReference,
                    md => md.Name == "set_MethodExecutionTag");
            var call = _creator.CallVoidInstanceMethod(setMethod, createMethodExecutionArgsInstance.Variable, value);

            var block = new NamedInstructionBlockChain(value, objectTypeRef);
            block.Add(call);
            return block;
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

        public InstructionBlockChain CallAspectOnEntry(NamedInstructionBlockChain createAspectInstance,
            NamedInstructionBlockChain newMethodExectionArgsBlockChain)
        {
            var onEntryMethodRef = _referenceFinder.GetMethodReference(createAspectInstance.TypeReference,
                md => md.Name == "OnEntry");
            var callOnEntryBlock = _creator.CallVoidInstanceMethod(onEntryMethodRef,
                createAspectInstance.Variable, newMethodExectionArgsBlockChain.Variable);

            var callAspectOnEntryBlockChain = new InstructionBlockChain();
            callAspectOnEntryBlockChain.Add(callOnEntryBlock);
            return callAspectOnEntryBlockChain;
        }

        public InstructionBlockChain CallAspectOnExit(NamedInstructionBlockChain createAspectInstance,
            NamedInstructionBlockChain newMethodExectionArgsBlockChain)
        {
            var onExitMethodRef = _referenceFinder.GetMethodReference(createAspectInstance.TypeReference,
                md => md.Name == "OnExit");
            var callOnExitBlock = _creator.CallVoidInstanceMethod(onExitMethodRef,
                createAspectInstance.Variable, newMethodExectionArgsBlockChain.Variable);

            var callAspectOnExitBlockChain = new InstructionBlockChain();
            callAspectOnExitBlockChain.Add(callOnExitBlock);
            return callAspectOnExitBlockChain;
        }

        public InstructionBlockChain CallAspectOnException(NamedInstructionBlockChain createAspectInstance,
            NamedInstructionBlockChain newMethodExectionArgsBlockChain)
        {
            var onExceptionMethodRef = _referenceFinder.GetMethodReference(createAspectInstance.TypeReference,
                md => md.Name == "OnException");
            var callOnExceptionBlock = _creator.CallVoidInstanceMethod(onExceptionMethodRef,
                createAspectInstance.Variable, newMethodExectionArgsBlockChain.Variable);

            var callAspectOnExceptionBlockChain = new InstructionBlockChain();
            callAspectOnExceptionBlockChain.Add(callOnExceptionBlock);
            return callAspectOnExceptionBlockChain;
        }

        public InstructionBlockChain CallMethodWithLocalParameters(MethodDefinition method,
            MethodDefinition targetMethod, VariableDefinition caller, VariableDefinition resultVariable)
        {
            var chain = new InstructionBlockChain();
            var variables = new List<VariableDefinition>();

            var index = 0;
            foreach (var parameter in method.Parameters)
            {
                var argIndex = method.IsStatic 
                    ? index 
                    : index + 1;
                var push = _creator.LoadValueOnStackFromArguments(argIndex);
                chain.Add(new InstructionBlock("push" + index, push.ToList()));

                var variable = _creator.CreateVariable(parameter.ParameterType);
                variables.Add(variable);
                var assign = _creator.AssignValueFromStack(variable);
                chain.Add(assign);

                index++;
            }

            var type = FixTypeReference(targetMethod.DeclaringType);
            var targetMethodReference = FixMethodReference(targetMethod);
            var call = caller == null
                ? _creator.CallStaticMethod(targetMethodReference, resultVariable, variables.ToArray())
                : _creator.CallInstanceMethod(targetMethodReference, caller, resultVariable, variables.ToArray());
            chain.Add(call);

            return chain;
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

        private static MethodReference FixMethodReference(MethodReference targetMethod)
        {
            if (!targetMethod.HasGenericParameters) 
                return targetMethod;

            // workaround for method in generic type (slightly modified, shorter and better :)
            // https://stackoverflow.com/questions/4968755/mono-cecil-call-generic-base-class-method-from-other-assembly

            return targetMethod.MakeGeneric();
        }
    }
}