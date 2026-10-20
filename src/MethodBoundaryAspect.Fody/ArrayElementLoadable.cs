using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    public class ArrayElementLoadable : ILoadable
    {
        private readonly VariableDefinition _arrayVariableDefinition;
        private readonly int _index;
        private readonly ParameterDefinition _parameter;
        private readonly ILProcessor _processor;
        private readonly InstructionBlockChainCreator _creator;

        public ArrayElementLoadable(
            VariableDefinition arrayVariableDefinition,
            int index,
            ParameterDefinition parameterDefinition,
            ILProcessor methodProcessor, InstructionBlockChainCreator creator)
        {
            _arrayVariableDefinition = arrayVariableDefinition;
            _index = index;
            _parameter = parameterDefinition;
            _processor = methodProcessor;
            _creator = creator;
        }

        public TypeReference PersistedType => _parameter.ParameterType;

        /// <summary>
        /// Local variable whose address was passed for a ref/out parameter by the last <see cref="Load"/> call.
        /// </summary>
        public VariableDefinition ByRefVariable { get; private set; }

        public InstructionBlock Load(bool forDereferencing, bool onlyValue)
        {
            var instructions = new List<Instruction>
            {
                _processor.Create(OpCodes.Ldloc, _arrayVariableDefinition),
                _processor.Create(OpCodes.Ldc_I4, _index),
                _processor.Create(OpCodes.Ldelem_Ref)
            };

            var arrayTypeReference = (ArrayType)_arrayVariableDefinition.VariableType;
            var castFromType = arrayTypeReference.ElementType;

            var parameterType = _parameter.ParameterType.WithoutModifiers();
            var castToType = parameterType;
            if (parameterType.IsByReference) // support for ref-Arguments
                castToType = ((ByReferenceType) castToType).ElementType;

            var castOrUnbox = InstructionBlockCreator.CastValueCurrentlyOnStack(
                castFromType,
                castToType);
            instructions.AddRange(castOrUnbox);
            if (instructions[instructions.Count - 1].OpCode == OpCodes.Unbox_Any)
                MakeNullTheDefaultValue(instructions, castToType);

            if (!onlyValue && parameterType.IsByReference) // support for ref-Arguments
            {
                var variable = _creator.CreateVariable(castToType).Variable;
                instructions.Add(_processor.Create(OpCodes.Stloc, variable));
                instructions.Add(_processor.Create(OpCodes.Ldloca, variable));
                ByRefVariable = variable;
            }

            return new InstructionBlock($"Load {_index}", instructions);
        }

        // an aspect can set the element of a value type to null: unbox.any would throw a NullReferenceException,
        // the argument is the default value of the type instead
        private void MakeNullTheDefaultValue(List<Instruction> instructions, TypeReference valueType)
        {
            var unbox = instructions[instructions.Count - 1];
            instructions.RemoveAt(instructions.Count - 1);

            var defaultValue = _creator.CreateVariable(valueType).Variable;
            var unboxLabel = unbox;
            var end = _processor.Create(OpCodes.Nop);
            instructions.Add(_processor.Create(OpCodes.Dup));
            instructions.Add(_processor.Create(OpCodes.Brtrue, unboxLabel));
            instructions.Add(_processor.Create(OpCodes.Pop));
            instructions.Add(_processor.Create(OpCodes.Ldloca, defaultValue));
            instructions.Add(_processor.Create(OpCodes.Initobj, valueType));
            instructions.Add(_processor.Create(OpCodes.Ldloc, defaultValue));
            instructions.Add(_processor.Create(OpCodes.Br, end));
            instructions.Add(unbox);
            instructions.Add(end);
        }
    }
}