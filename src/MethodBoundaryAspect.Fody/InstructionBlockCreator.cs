using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    public class InstructionBlockCreator
    {
        private const string VoidType = "Void";

        private readonly MethodDefinition _method;
        private readonly ReferenceFinder _referenceFinder;

        private readonly ILProcessor _processor;

        public InstructionBlockCreator(MethodDefinition method, ReferenceFinder referenceFinder)
        {
            _method = method;
            _referenceFinder = referenceFinder;
            _processor = _method.Body.GetILProcessor();
        }
        
        public VariableDefinition CreateVariable(TypeReference variableTypeReference)
        {
            if (IsVoid(variableTypeReference))
                throw new InvalidOperationException("Variable of type 'Void' is not possible!");

            var variableDefinition = new VariableDefinition(variableTypeReference);
            _method.Body.Variables.Add(variableDefinition);
            return variableDefinition;
        }

        public InstructionBlock CreateThisVariable(VariableDefinition instanceVariable)
        {
            if (_method.IsStatic)
                return new InstructionBlock("Static method call: " + _method.Name, _processor.Create(OpCodes.Nop));
            
            var loadThisInstruction = _processor.Create(OpCodes.Ldarg_0);
            var storeThisInstruction = _processor.Create(OpCodes.Stloc, instanceVariable);

            return new InstructionBlock(
                "Store instance for: " + _method.Name,
                loadThisInstruction,
                storeThisInstruction);
        }

        public InstructionBlock CreateObjectArrayWithMethodArguments(VariableDefinition objectArray,
            TypeReference objectTypeReference)
        {
            var createArrayInstructions = new List<Instruction>
            {
                _processor.Create(OpCodes.Ldc_I4, _method.Parameters.Count), //method.Parameters.Count
                _processor.Create(OpCodes.Newarr, objectTypeReference), // new object[method.Parameters.Count]
                _processor.Create(OpCodes.Stloc, objectArray) // var objArray = new object[method.Parameters.Count]
            };

            foreach (var parameter in _method.Parameters)
            {
                var boxedArguments = BoxArguments(parameter, objectArray).ToList();
                createArrayInstructions.AddRange(boxedArguments);
            }

            return new InstructionBlock("CreateObjectArrayWithMethodArguments", createArrayInstructions);
        }

        public InstructionBlock NewObject(
            VariableDefinition newInstance,
            TypeReference instanceTypeReference,
            ModuleDefinition module)
        {
            return NewObject(newInstance, instanceTypeReference, module, null);
        }
        
        public InstructionBlock NewObject(
            VariableDefinition newInstance,
            TypeReference instanceTypeReference,
            ModuleDefinition module,
            CustomAttribute aspect)
        {
            var typeDefinition = instanceTypeReference.Resolve();
            var constructor = typeDefinition
                .Methods
                .SingleOrDefault(x => x.IsConstructor &&
                            !x.IsStatic &&
                            x.Parameters.Select(p => p.ParameterType.FullName)
                                .SequenceEqual(aspect?.ConstructorArguments.Select(a => a.Type.FullName) ?? new string[0]));

            if (constructor == null)
                throw new InvalidOperationException(string.Format("Didn't find matching constructor on type '{0}'",
                    typeDefinition.FullName));

            var ctorRef = module.ImportReference(constructor);
            var newObjectInstruction = _processor.Create(OpCodes.Newobj, ctorRef);
            var assignToVariableInstruction = _processor.Create(OpCodes.Stloc, newInstance);

            var loadConstValuesOnStack = new List<Instruction>();
            var loadSetConstValuesToAspect = new List<Instruction>();
            if (aspect != null)
            {
                // ctor parameters
                var loadInstructions = aspect.Constructor.Parameters
                    .Zip(aspect.ConstructorArguments, (p, v) => new {Parameter = p, Value = v})
                    .SelectMany(x => LoadValueOnStack(x.Parameter.ParameterType, x.Value.Value))
                    .ToList();
                loadConstValuesOnStack.AddRange(loadInstructions);

                // named arguments
                foreach (var property in aspect.Properties)
                {
                    var propertyCopy = property;
                    var propertyType = module.ImportReference(propertyCopy.Argument.Type.Resolve());

                    var loadOnStackInstruction = LoadValueOnStack(propertyType, propertyCopy.Argument.Value);
                    var valueVariable = CreateVariable(propertyType);
                    var assignVariableInstructionBlock = AssignValueFromStack(valueVariable);

                    var methodRef = _referenceFinder.GetMethodReference(instanceTypeReference, md => md.Name == "set_" + propertyCopy.Name);
                    var setPropertyInstructionBlock = CallVoidInstanceMethod(methodRef,
                        new VariablePersistable(newInstance),
                        new VariablePersistable(valueVariable));

                    loadSetConstValuesToAspect.AddRange(loadOnStackInstruction);
                    loadSetConstValuesToAspect.AddRange(assignVariableInstructionBlock.Instructions);
                    loadSetConstValuesToAspect.AddRange(setPropertyInstructionBlock.Instructions);
                }

                foreach (var field in aspect.Fields)
                {
                    var fieldCopy = field;

                    var loadInstanceOnStackInstruction = _processor.Create(OpCodes.Ldloc, newInstance);
                    var loadOnStackInstruction = LoadValueOnStack(fieldCopy.Argument.Type, fieldCopy.Argument.Value);

                    var fieldRef = instanceTypeReference.Resolve().Fields.First(f => f.Name == fieldCopy.Name);
                    var loadFieldInstructionBlock = _processor.Create(OpCodes.Stfld, fieldRef);

                    loadSetConstValuesToAspect.Add(loadInstanceOnStackInstruction);
                    loadSetConstValuesToAspect.AddRange(loadOnStackInstruction);
                    loadSetConstValuesToAspect.Add(loadFieldInstructionBlock);
                }
            }

            var allInstructions = new List<Instruction>();
            allInstructions.AddRange(loadConstValuesOnStack);
            allInstructions.Add(newObjectInstruction);
            allInstructions.Add(assignToVariableInstruction);
            allInstructions.AddRange(loadSetConstValuesToAspect);

            return new InstructionBlock(
                "NewObject: " + instanceTypeReference.Name,
                allInstructions);
        }

        public InstructionBlock SaveReturnValueFromStack(VariableDefinition returnValue)
        {
            if (IsVoid(_method.ReturnType))
                return new InstructionBlock("SaveReturnValueFromStack - void", _processor.Create(OpCodes.Nop));

            var assignReturnValueToVariableInstruction = _processor.Create(OpCodes.Stloc, returnValue);
            return new InstructionBlock(
                "SaveReturnValueFromStack",
                assignReturnValueToVariableInstruction);
        }

        public InstructionBlock AssignValueFromStack(VariableDefinition variable)
        {
            return new InstructionBlock(
                "AssignValueFromStack",
                _processor.Create(OpCodes.Stloc, variable));
        }

        public InstructionBlock PushValueOnStack(VariableDefinition variable)
        {
            return new InstructionBlock(
                "PushValueOnStack",
                _processor.Create(OpCodes.Ldloc, variable));
        }
        
        public InstructionBlock CallVoidInstanceMethod(
            MethodReference methodReference,
            IPersistable callInstanceVariable,
            params ILoadable[] argumentVariables)
        {
            var instructions = CreateInstanceMethodCallInstructions(methodReference,
                callInstanceVariable, null, argumentVariables);
            return new InstructionBlock("CallVoidInstanceMethod: " + methodReference.Name, instructions);
        }

        public InstructionBlock CallStaticMethod(
            MethodReference methodReference,
            IPersistable returnValue,
            params ILoadable[] argumentVariables)
        {
            var instructions = CreateMethodCallInstructions(methodReference, null, returnValue, argumentVariables);
            return new InstructionBlock("CallStaticMethod: " + methodReference.Name, instructions);
        }

        public InstructionBlock CallInstanceMethod(
            MethodReference methodReference,
            ILoadable callerInstance,
            IPersistable returnValue,
            params ILoadable[] arguments)
        {
            var instructions = CreateInstanceMethodCallInstructions(methodReference,
                callerInstance, returnValue, arguments);
            return new InstructionBlock("CallInstanceMethod: " + methodReference.Name, instructions);
        }

        private static List<Instruction> CreateInstanceMethodCallInstructions(
            MethodReference methodReference,
            ILoadable callerInstance,
            IPersistable returnValue,
            params ILoadable[] arguments)
        {
            return CreateMethodCallInstructions(methodReference, callerInstance, returnValue, arguments);
        }

        private static List<Instruction> CreateMethodCallInstructions(
            MethodReference methodReference,
            ILoadable instance,
            IPersistable returnValue,
            params ILoadable[] arguments)
        {
            InstructionBlock loadVariableInstruction = null;
            if (instance != null)
                loadVariableInstruction = instance.Load(true, false);

            var loadArgumentsInstructions = new List<Instruction>();
            var parameterCount = 0;
            foreach (var argument in arguments)
            {
                var paramIsByRef = methodReference.Parameters[parameterCount].ParameterType.IsByReference;

                loadArgumentsInstructions.AddRange(argument.Load(false, false).Instructions);
                var varType = argument.PersistedType;
                if (varType.IsByReference && !paramIsByRef)
                {
                    varType = ((ByReferenceType)varType).ElementType;
                    loadArgumentsInstructions.Add(Instruction.Create(OpCodes.Ldobj, varType));
                }

                var parameter = methodReference.Parameters[parameterCount];
                if (parameter.ParameterType.FullName != varType.FullName)
                {
                    if (varType.IsValueType || varType.IsGenericParameter)
                        loadArgumentsInstructions.Add(Instruction.Create(OpCodes.Box, varType));
                }
                parameterCount++;
            }

            var methodDefinition = methodReference.Resolve();
            var methodCallInstruction = Instruction.Create(
                methodDefinition.IsVirtual
                    ? OpCodes.Callvirt
                    : OpCodes.Call,
                methodReference);

            var methodReturnsVoid = IsVoid(methodDefinition.ReturnType);
            var returnValueShouldBeStored = (returnValue != null);

            List<Instruction> HandleReturnValue(List<Instruction> methodWork)
            {
                if (methodReturnsVoid && returnValueShouldBeStored)
                    throw new InvalidOperationException("Method has no return value");
                if (!methodReturnsVoid && !returnValueShouldBeStored)
                {
                    methodWork.Add(Instruction.Create(OpCodes.Pop));
                    return methodWork;
                }
                if (methodReturnsVoid && !returnValueShouldBeStored)
                    return methodWork;
                // Therefore, !methodReturnsVoid && returnValueShouldBeStored
                
                var castToType = returnValue.PersistedType;
                var castFromType = methodDefinition.ReturnType;
                if (castToType.FullName != castFromType.FullName)
                {
                    if (castToType.IsByReference)
                        castToType = ((ByReferenceType)castToType).ElementType;

                    if (castFromType.IsByReference)
                        castFromType = ((ByReferenceType)castFromType).ElementType;
                    methodWork.AddRange(CastValueCurrentlyOnStack(castFromType, castToType));
                }
                return returnValue.Store(new InstructionBlock("", methodWork), castToType).Instructions.ToList();
            }
            
            var instructions = new List<Instruction>();
            if (loadVariableInstruction != null)
                instructions.AddRange(loadVariableInstruction.Instructions);
            instructions.AddRange(loadArgumentsInstructions);
            instructions.Add(methodCallInstruction);
            return HandleReturnValue(instructions);
        }

        public static IEnumerable<Instruction> CastValueCurrentlyOnStack(TypeReference fromType, TypeReference toType)
        {
            if (fromType.Equals(toType) || fromType.FullName.Equals(toType.FullName))
                return Enumerable.Empty<Instruction>();

            if (fromType.FullName == typeof(object).FullName)
            {
                if (toType.IsValueType || toType.IsGenericParameter)
                    return new[] { Instruction.Create(OpCodes.Unbox_Any, toType) };
                
                return new[] { Instruction.Create(OpCodes.Castclass, toType) };
            }

            throw new NotSupportedException("Cannot unbox non-object types.");
        }

        private IList<Instruction> LoadValueOnStack(TypeReference parameterType, object value)
        {
            if (parameterType.IsArray(out var elementType) && value is CustomAttributeArgument[] args)
            {
                elementType = elementType.CleanEnumsInTypeRef();
                parameterType = parameterType.CleanEnumsInTypeRef();
                
                var array = CreateVariable(parameterType);
                var createArrayInstructions = new List<Instruction>()
                {
                    _processor.Create(OpCodes.Ldc_I4, args.Length),
                    _processor.Create(OpCodes.Newarr, elementType),
                    _processor.Create(OpCodes.Stloc, array)
                };

                var stelem = elementType.GetStElemCode();

                for (var i = 0; i < args.Length; ++i)
                {
                    var parameter = args[i];
                    createArrayInstructions.Add(_processor.Create(OpCodes.Ldloc, array));
                    createArrayInstructions.Add(_processor.Create(OpCodes.Ldc_I4, i));
                    createArrayInstructions.AddRange(LoadValueOnStack(elementType, parameter.Value));
                    createArrayInstructions.Add(_processor.Create(stelem));
                }

                createArrayInstructions.Add(_processor.Create(OpCodes.Ldloc, array));
                return createArrayInstructions;
            }
            
            if (parameterType.IsEnum(out var enumUnderlyingType))
            {
                enumUnderlyingType = enumUnderlyingType.CleanEnumsInTypeRef();
                return new List<Instruction>(LoadPrimitiveConstOnStack(enumUnderlyingType.MetadataType, value));
            }
            
            if (parameterType.IsPrimitive || (parameterType.FullName == typeof(String).FullName))
                return new List<Instruction>(LoadPrimitiveConstOnStack(parameterType.MetadataType, value));

            if (parameterType.FullName == typeof(Type).FullName)
            {
                var typeVal = (TypeReference)value;
                var typeReference = typeVal.CleanEnumsInTypeRef();

                var typeTypeRef = _referenceFinder.GetTypeReference(typeof(Type));
                var methodReference = _referenceFinder.GetMethodReference(typeTypeRef, md => md.Name == nameof(Type.GetTypeFromHandle));

                var instructions = new List<Instruction>
                {
                    _processor.Create(OpCodes.Ldtoken, typeReference),
                    _processor.Create(OpCodes.Call, methodReference)
                };

                return instructions;
            }

            if (parameterType.FullName == typeof(Object).FullName && value is CustomAttributeArgument arg)
            {
                var valueType = arg.Type;
                if (arg.Value is TypeReference)
                    valueType = _referenceFinder.GetTypeReference(typeof(Type));
                var isEnum = valueType.IsEnum(out _);
                valueType = valueType.CleanEnumsInTypeRef();
                var instructions = LoadValueOnStack(valueType, arg.Value);
                if (valueType.IsValueType || (!valueType.IsArray && isEnum))
                    instructions.Add(_processor.Create(OpCodes.Box, valueType));
                return instructions;
            }

            throw new NotSupportedException("Parametertype: " + parameterType);
        }

        private IEnumerable<Instruction> LoadPrimitiveConstOnStack(MetadataType type, object value)
        {
            switch (type)
            {
                case MetadataType.String when (value == null):
                    return new[] { _processor.Create(OpCodes.Ldnull) };
                case MetadataType.String:
                    return new[] { _processor.Create(OpCodes.Ldstr, (string)value) };
                case MetadataType.Byte:
                    return new[] { _processor.Create(OpCodes.Ldc_I4, (int)(byte)value) };
                case MetadataType.SByte:
                    return new[] { _processor.Create(OpCodes.Ldc_I4, (int)(sbyte)value) };
                case MetadataType.Int16:
                    return new[] { _processor.Create(OpCodes.Ldc_I4, (int)(short)value) };
                case MetadataType.UInt16:
                    return new[] { _processor.Create(OpCodes.Ldc_I4, (int)(ushort)value) };
                case MetadataType.Int32:
                    return new[] { _processor.Create(OpCodes.Ldc_I4, (int)value) };
                case MetadataType.UInt32:
                    return new[] { _processor.Create(OpCodes.Ldc_I4, (int)(uint)value) };
                case MetadataType.Int64:
                    var longVal = (long)value;
                    if (longVal <= Int32.MaxValue && longVal >= Int32.MinValue)
                        return new[]
                        {
                            _processor.Create(OpCodes.Ldc_I4, (int)longVal),
                            _processor.Create(OpCodes.Conv_I8)
                        };
                    return new[] { _processor.Create(OpCodes.Ldc_I8, longVal) };
                case MetadataType.UInt64:
                    var ulongVal = (ulong)value;
                    if (ulongVal <= Int32.MaxValue)
                        return new[]
                        {
                            _processor.Create(OpCodes.Ldc_I4, (int)ulongVal),
                            _processor.Create(OpCodes.Conv_I8)
                        };
                    return new[] { _processor.Create(OpCodes.Ldc_I8, (long)ulongVal) };
                case MetadataType.Boolean:
                    return new[] { _processor.Create(OpCodes.Ldc_I4, (bool)value ? 1 : 0) };
            }

            throw new NotSupportedException("Not a supported primitve parameter type: " + type);
        }

        private static IEnumerable<Instruction> BoxArguments(
            ParameterDefinition parameterDefinition,
            VariableDefinition paramsArray)
        {

            var paramMetaData = parameterDefinition.ParameterType.MetadataType;
            if (paramMetaData == MetadataType.UIntPtr ||
                paramMetaData == MetadataType.FunctionPointer ||
                paramMetaData == MetadataType.IntPtr ||
                paramMetaData == MetadataType.Pointer)
            {
                yield break;
            }

            yield return Instruction.Create(OpCodes.Ldloc, paramsArray);
            yield return Instruction.Create(OpCodes.Ldc_I4, parameterDefinition.Index);
            yield return Instruction.Create(OpCodes.Ldarg, parameterDefinition);
            // Reset boolean flag variable to false

            // If a parameter is passed by reference then you need to use Ldind
            // ------------------------------------------------------------
            var paramType = parameterDefinition.ParameterType;

            if (paramType.IsByReference)
            {
                var referencedTypeSpec = (TypeSpecification) paramType;

                var pointerToValueTypeVariable = false;
                
                switch (referencedTypeSpec.ElementType.MetadataType)
                {
                        //Indirect load value of type int8 as int32 on the stack
                    case MetadataType.Boolean:
                    case MetadataType.SByte:
                        yield return Instruction.Create(OpCodes.Ldind_I1);
                        pointerToValueTypeVariable = true;
                        break;

                        // Indirect load value of type int16 as int32 on the stack
                    case MetadataType.Int16:
                        yield return Instruction.Create(OpCodes.Ldind_I2);
                        pointerToValueTypeVariable = true;
                        break;

                        // Indirect load value of type int32 as int32 on the stack
                    case MetadataType.Int32:
                        yield return Instruction.Create(OpCodes.Ldind_I4);
                        pointerToValueTypeVariable = true;
                        break;

                        // Indirect load value of type int64 as int64 on the stack
                        // Indirect load value of type unsigned int64 as int64 on the stack (alias for ldind.i8)
                    case MetadataType.Int64:
                    case MetadataType.UInt64:
                        yield return Instruction.Create(OpCodes.Ldind_I8);
                        pointerToValueTypeVariable = true;
                        break;

                        // Indirect load value of type unsigned int8 as int32 on the stack
                    case MetadataType.Byte:
                        yield return Instruction.Create(OpCodes.Ldind_U1);
                        pointerToValueTypeVariable = true;
                        break;

                        // Indirect load value of type unsigned int16 as int32 on the stack
                    case MetadataType.UInt16:
                    case MetadataType.Char:
                        yield return Instruction.Create(OpCodes.Ldind_U2);
                        pointerToValueTypeVariable = true;
                        break;

                        // Indirect load value of type unsigned int32 as int32 on the stack
                    case MetadataType.UInt32:
                        yield return Instruction.Create(OpCodes.Ldind_U4);
                        pointerToValueTypeVariable = true;
                        break;

                        // Indirect load value of type float32 as F on the stack
                    case MetadataType.Single:
                        yield return Instruction.Create(OpCodes.Ldind_R4);
                        pointerToValueTypeVariable = true;
                        break;

                        // Indirect load value of type float64 as F on the stack
                    case MetadataType.Double:
                        yield return Instruction.Create(OpCodes.Ldind_R8);
                        pointerToValueTypeVariable = true;
                        break;

                        // Indirect load value of type native int as native int on the stack
                    case MetadataType.IntPtr:
                    case MetadataType.UIntPtr:
                        yield return Instruction.Create(OpCodes.Ldind_I);
                        pointerToValueTypeVariable = true;
                        break;

                    default:
                        // Need to check if it is a value type instance, in which case
                        // we use Ldobj instruction to copy the contents of value type
                        // instance to stack and then box it
                        if (referencedTypeSpec.ElementType.IsValueType || referencedTypeSpec.ElementType.IsGenericParameter)
                        {
                            yield return Instruction.Create(OpCodes.Ldobj, referencedTypeSpec.ElementType);
                            pointerToValueTypeVariable = true;
                        }
                        else
                        {
                            // It is a reference type so just use reference the pointer
                            yield return Instruction.Create(OpCodes.Ldind_Ref);
                        }
                        break;
                }

                if (pointerToValueTypeVariable)
                {
                    // Box the de-referenced parameter type
                    yield return Instruction.Create(OpCodes.Box, referencedTypeSpec.ElementType);
                }

            }
            else
            {

                // If it is a value type then you need to box the instance as we are going 
                // to add it to an array which is of type object (reference type)
                // ------------------------------------------------------------
                if (paramType.IsValueType || paramType.IsGenericParameter)
                {
                    // Box the parameter type
                    yield return Instruction.Create(OpCodes.Box, paramType);
                }
                else if (paramType.Name == "String")
                {
                    //var elementType = paramsArray.VariableType.GetElementType();
                    //yield return Instruction.Create(OpCodes.Castclass, elementType);
                }
            }

            // Store parameter in object[] array
            // ------------------------------------------------------------
            yield return Instruction.Create(OpCodes.Stelem_Ref);
        }

        public static bool IsVoid(TypeReference type)
        {
            return type.Name == VoidType;
        }

        public Instruction CreateReturn()
        {
            return _processor.Create(OpCodes.Ret);
        }
    }
}