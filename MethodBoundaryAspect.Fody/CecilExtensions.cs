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
    /// taken from https://github.com/Catel/Catel.Fody/blob/develop/src/Catel.Fody/Extensions/CecilExtensions.debuginfo.cs#L23
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
                var hasVariable = scope.Variables.Any(x => x.Index == variable.Index);
                if (!hasVariable)
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

            // Step 3: update the scopes by setting the indices
            scope.Start = new InstructionOffset(instructions.First());
            scope.End = new InstructionOffset(instructions.Last());
        }
    }
}
namespace MethodBoundaryAspect.Fody
{
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public static class CecilExtensions
    {
        public static OpCode GetStElemCode(this MetadataType type)
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
    }
}