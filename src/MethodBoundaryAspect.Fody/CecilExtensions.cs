// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CecilExtensions.debuginfo.cs" company="Catel development team">
//   Copyright (c) 2008 - 2017 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


// ReSharper disable once CheckNamespace
namespace Catel.Fody
{
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// taken from https://github.com/Catel/Catel.Fody/blob/master/src/Catel.Fody/Extensions/CecilExtensions.debuginfo.cs#L23
    /// </summary>
    public static partial class CecilExtensions
    {
        private const long AddressToIgnore = 16707566;

        private static readonly FieldInfo SequencePointOffsetFieldInfo = typeof(SequencePoint).GetField("offset", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo InstructionOffsetInstructionFieldInfo = typeof(InstructionOffset).GetField("instruction", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void UpdateDebugInfo(this MethodDefinition method)
        {
            var debugInfo = method.DebugInformation;
            var instructions = method.Body.Instructions;
            var scope = debugInfo.Scope;

            if (scope == null || instructions.Count == 0)
            {
                return;
            }

            var oldSequencePoints = debugInfo.SequencePoints;
            var newSequencePoints = new Collection<SequencePoint>();

            // Step 1: check if all variables are present
            foreach (var variable in method.Body.Variables)
            {
                if (method.IsAsyncMethod())
                {
                    // Skip some special items of an async method:
                    // 1) int (state?)
                    // 2) exception
                    if (variable.Index == 0 && variable.VariableType.Name.Contains("Int") ||
                        variable.VariableType.Name.Contains("Exception"))
                    {
                        continue;
                    }
                }

                if (!ContainsVariable(scope, variable))
                {
                    var variableDebugInfo = new VariableDebugInformation(variable, $"__var_{variable.Index}");
                    scope.Variables.Add(variableDebugInfo);
                }
            }

            // Step 2: Make sure the instructions point to the correct items
            foreach (var oldSequencePoint in oldSequencePoints)
            {
                //var isValid = false;

                //// Special cases we need to ignore
                //if (oldSequencePoint.StartLine == AddressToIgnore ||
                //    oldSequencePoint.EndLine == AddressToIgnore)
                //{
                //    continue;
                //}

                var instructionOffset = (InstructionOffset)SequencePointOffsetFieldInfo.GetValue(oldSequencePoint);
                var offsetInstruction = (Instruction)InstructionOffsetInstructionFieldInfo.GetValue(instructionOffset);

                // Fix offset
                for (var i = 0; i < instructions.Count; i++)
                {
                    var instruction = instructions[i];
                    if (instruction == offsetInstruction)
                    {
                        var newSequencePoint = new SequencePoint(instruction, oldSequencePoint.Document)
                        {
                            StartLine = oldSequencePoint.StartLine,
                            StartColumn = oldSequencePoint.StartColumn,
                            EndLine = oldSequencePoint.EndLine,
                            EndColumn = oldSequencePoint.EndColumn
                        };

                        newSequencePoints.Add(newSequencePoint);

                        //isValid = true;

                        break;
                    }
                }
            }

            debugInfo.SequencePoints.Clear();

            foreach (var newSequencePoint in newSequencePoints)
            {
                debugInfo.SequencePoints.Add(newSequencePoint);
            }

            // Step 3: Remove any unused variables
            RemoveUnusedVariablesFromDebugInfo(scope);

            // Final step: update the scopes by setting the indices
            scope.Start = new InstructionOffset(instructions.First());
            scope.End = new InstructionOffset(instructions.Last());
        }

        public static bool IsAsyncMethod(this MethodDefinition method)
        {
            if (!method.Name.Contains("MoveNext"))
            {
                return false;
            }

            var declaringType = method.DeclaringType;

            var setStateMachineMethod = declaringType?.Methods.FirstOrDefault(x => x.Name.Equals("SetStateMachine"));
            if (setStateMachineMethod == null)
            {
                return false;
            }

            return true;
        }

        private static bool ContainsVariable(this ScopeDebugInformation debugInfo, VariableDefinition variable)
        {
            // Note: just checking for index might not be sufficient
            var hasVariable = debugInfo.Variables.Any(x => x.Index == variable.Index);
            if (hasVariable)
            {
                return true;
            }

            // Important: check nested scopes
            for (var i = 0; i < debugInfo.Scopes.Count; i++)
            {
                if (ContainsVariable(debugInfo.Scopes[i], variable))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveUnusedVariablesFromDebugInfo(this ScopeDebugInformation debugInfo)
        {
            // Remove any variables that no longer have a valid index (such as -1)
            for (var i = 0; i < debugInfo.Variables.Count; i++)
            {
                var debugInfoVariable = debugInfo.Variables[i];
                if (debugInfoVariable.Index < 0)
                {
                    debugInfo.Variables.Remove(debugInfoVariable);
                    i--;
                }
            }

            // Important: nested scopes (for example, for async methods)
            for (var i = 0; i < debugInfo.Scopes.Count; i++)
            {
                RemoveUnusedVariablesFromDebugInfo(debugInfo.Scopes[i]);
            }
        }
    }
}
namespace MethodBoundaryAspect.Fody
{
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class CecilExtensions
    {
        public static TypeReference CleanEnumsInTypeRef(this TypeReference type)
        {
            if (type.IsArray(out TypeReference elementType))
            {
                elementType = CleanEnumsInTypeRef(elementType);
                return new ArrayType(elementType);
            }

            if (type.IsEnum(out _))
            {
                if (!type.IsValueType)
                    type.IsValueType = true;
                return type;
            }

            if (type is GenericInstanceType gen)
            {
                for (int i = 0; i < gen.GenericArguments.Count; ++i)
                    gen.GenericArguments[i] = CleanEnumsInTypeRef(gen.GenericArguments[i]);
                return gen;
            }

            return type;
        }

        public static bool IsArray(this TypeReference typeRef, out TypeReference elementType)
        {
            elementType = null;
            if (!typeRef.IsArray)
                return false;

            elementType = ((ArrayType)typeRef).ElementType;
            return true;
        }

        public static bool IsEnum(this TypeReference typeRef, out TypeReference underlyingType)
        {
            var typeDef = typeRef.Resolve();
            if (typeDef.IsEnum)
            {
                underlyingType = typeDef.Fields.FirstOrDefault(f => f.Name == "value__").FieldType;
                return true;
            }

            underlyingType = null;
            return false;
        }

        public static OpCode GetStElemCode(this TypeReference typeRef)
        {
            var typeDef = typeRef.Resolve();
            if (typeDef.IsEnum(out TypeReference underlying))
                return underlying.MetadataType.GetStElemCode();
            if (typeRef.IsValueType)
                return typeRef.MetadataType.GetStElemCode();
            return OpCodes.Stelem_Ref;
        }

        static OpCode GetStElemCode(this MetadataType type)
        {
            switch (type)
            {
                case MetadataType.Boolean:
                case MetadataType.Int32:
                case MetadataType.UInt32:
                    return OpCodes.Stelem_I4;
                case MetadataType.Byte:
                case MetadataType.SByte:
                    return OpCodes.Stelem_I1;
                case MetadataType.Char:
                case MetadataType.Int16:
                case MetadataType.UInt16:
                    return OpCodes.Stelem_I2;
                case MetadataType.Double:
                    return OpCodes.Stelem_R8;
                case MetadataType.Int64:
                case MetadataType.UInt64:
                    return OpCodes.Stelem_I8;
                case MetadataType.Single:
                    return OpCodes.Stelem_R4;
                default:
                    return OpCodes.Stelem_Ref;
            }
        }
        
        public static Instruction GetStIndInstruction(this TypeReference typeRef)
        {
            if (typeRef.IsGenericParameter)
                return Instruction.Create(OpCodes.Stobj, typeRef);
            var typeDef = typeRef.Resolve();
            if (typeDef.IsEnum(out TypeReference underlying))
                return Instruction.Create(underlying.MetadataType.GetStIndCode());
            if (typeRef.IsValueType)
                return Instruction.Create(typeRef.MetadataType.GetStIndCode());
            return Instruction.Create(OpCodes.Stind_Ref);
        }

        public static OpCode GetStIndCode(this MetadataType type)
        {
            switch (type)
            {
                case MetadataType.Boolean:
                case MetadataType.Int32:
                case MetadataType.UInt32:
                    return OpCodes.Stind_I4;
                case MetadataType.Byte:
                case MetadataType.SByte:
                    return OpCodes.Stind_I1;
                case MetadataType.Char:
                case MetadataType.Int16:
                case MetadataType.UInt16:
                    return OpCodes.Stind_I2;
                case MetadataType.Double:
                    return OpCodes.Stind_R8;
                case MetadataType.Int64:
                case MetadataType.UInt64:
                    return OpCodes.Stind_I8;
                case MetadataType.Single:
                    return OpCodes.Stind_R4;
                default:
                    return OpCodes.Stind_Ref;
            }
        }

        public static FieldReference AddPublicInstanceField(this TypeReference typeRef, TypeReference fieldType)
        {
            var typeDef = typeRef.Resolve();
            var field = new FieldDefinition(typeDef.NextAllowableMemberName(), FieldAttributes.Public, fieldType);
            field.DeclaringType = typeDef;
            typeDef.Fields.Add(field);
            return new FieldReference(field.Name, field.FieldType, typeRef);
        }

        static string NextAllowableMemberName(this TypeDefinition typeDef)
        {
            var takenNames = new List<string>();
            takenNames.AddRange(typeDef.Fields.Select(f => f.Name));
            takenNames.AddRange(typeDef.Properties.Select(f => f.Name));
            takenNames.AddRange(typeDef.Methods.Select(f => f.Name));
            takenNames.AddRange(typeDef.NestedTypes.Select(t => t.Name));
            takenNames.Add(typeDef.Name);
            var setOfTakenNames = new HashSet<string>(takenNames);
            
            int i = 0;
            string proposal;
            do
                proposal = $"var_{++i}";
            while (setOfTakenNames.Contains(proposal));

            return proposal;
        }

        public static VariableDefinition GetLocalStoredByInstruction(this Instruction i, Mono.Collections.Generic.Collection<VariableDefinition> locals)
        {
            if (i.OpCode == OpCodes.Stloc || i.OpCode == OpCodes.Stloc_S)
                return i.Operand as VariableDefinition;
            if (i.OpCode == OpCodes.Stloc_0)
                return locals[0];
            if (i.OpCode == OpCodes.Stloc_1)
                return locals[1];
            if (i.OpCode == OpCodes.Stloc_2)
                return locals[2];
            if (i.OpCode == OpCodes.Stloc_3)
                return locals[3];
            throw new InvalidOperationException("Unable to find variable reference for storage.");
        }

        public static FieldReference AsDefinedOn(this FieldReference field, TypeReference fullType)
        {
            return new FieldReference(field.Name, field.FieldType, fullType);
        }

        public static Instruction Clone(this Instruction i)
        {
            // Based on the table at https://stackoverflow.com/a/7215711
            switch (i.OpCode.Code)
            {
                case Code.Ldarg_S:
                case Code.Ldarga_S:
                case Code.Starg_S:
                case Code.Ldarg:
                case Code.Ldarga:
                case Code.Starg:
                    return Instruction.Create(i.OpCode, (ParameterDefinition)i.Operand);
                case Code.Ldloc_S:
                case Code.Ldloca_S:
                case Code.Stloc_S:
                case Code.Ldloc:
                case Code.Ldloca:
                case Code.Stloc:
                    return Instruction.Create(i.OpCode, (VariableDefinition)i.Operand);
                case Code.Unaligned:
                case Code.No:
                    return Instruction.Create(i.OpCode, (byte)i.Operand);
                case Code.Ldc_I4_S:
                    return Instruction.Create(i.OpCode, (sbyte)i.Operand);
                case Code.Ldc_I4:
                    return Instruction.Create(i.OpCode, (int)i.Operand);
                case Code.Ldc_I8:
                    return Instruction.Create(i.OpCode, (long)i.Operand);
                case Code.Ldc_R4:
                    return Instruction.Create(i.OpCode, (float)i.Operand);
                case Code.Ldc_R8:
                    return Instruction.Create(i.OpCode, (double)i.Operand);
                case Code.Jmp:
                case Code.Call:
                case Code.Newobj:
                case Code.Callvirt:
                case Code.Ldftn:
                case Code.Ldvirtftn:
                    return Instruction.Create(i.OpCode, (MethodReference)i.Operand);
                case Code.Calli:
                    return Instruction.Create(i.OpCode, (CallSite)i.Operand);
                case Code.Br_S:
                case Code.Brfalse_S:
                case Code.Brtrue_S:
                case Code.Beq_S:
                case Code.Bge_S:
                case Code.Bgt_S:
                case Code.Ble_S:
                case Code.Blt_S:
                case Code.Bne_Un_S:
                case Code.Bge_Un_S:
                case Code.Bgt_Un_S:
                case Code.Ble_Un_S:
                case Code.Blt_Un_S:
                case Code.Br:
                case Code.Brfalse:
                case Code.Brtrue:
                case Code.Beq:
                case Code.Bge:
                case Code.Bgt:
                case Code.Ble:
                case Code.Blt:
                case Code.Bne_Un:
                case Code.Bge_Un:
                case Code.Bgt_Un:
                case Code.Ble_Un:
                case Code.Blt_Un:
                case Code.Leave:
                case Code.Leave_S:
                    return Instruction.Create(i.OpCode, (Instruction)i.Operand);
                case Code.Switch:
                    return Instruction.Create(i.OpCode, (Instruction[])i.Operand);
                case Code.Cpobj:
                case Code.Ldobj:
                case Code.Castclass:
                case Code.Isinst:
                case Code.Unbox:
                case Code.Stobj:
                case Code.Box:
                case Code.Newarr:
                case Code.Ldelema:
                case Code.Ldelem_Any:
                case Code.Stelem_Any:
                case Code.Unbox_Any:
                case Code.Refanyval:
                case Code.Mkrefany:
                case Code.Initobj:
                case Code.Constrained:
                case Code.Sizeof:
                    return Instruction.Create(i.OpCode, (TypeReference)i.Operand);
                case Code.Ldstr:
                    return Instruction.Create(i.OpCode, (string)i.Operand);
                case Code.Ldfld:
                case Code.Ldflda:
                case Code.Stfld:
                case Code.Ldsfld:
                case Code.Ldsflda:
                case Code.Stsfld:
                    return Instruction.Create(i.OpCode, (FieldReference)i.Operand);
                case Code.Ldtoken:
                    switch (i.Operand)
                    {
                        case TypeReference type: return Instruction.Create(i.OpCode, type);
                        case FieldReference field: return Instruction.Create(i.OpCode, field);
                        case MethodReference method: return Instruction.Create(i.OpCode, method);
                        default: throw new NotSupportedException("Unknown token type.");
                    }
                default:
                    return Instruction.Create(i.OpCode);
            }
        }
    }
}