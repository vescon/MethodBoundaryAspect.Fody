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
        private readonly bool _passOriginalReference;

        public ArrayElementLoadable(
            VariableDefinition arrayVariableDefinition,
            int index,
            ParameterDefinition parameterDefinition,
            ILProcessor methodProcessor, InstructionBlockChainCreator creator,
            bool passOriginalReference = false)
        {
            _passOriginalReference = passOriginalReference;
            _arrayVariableDefinition = arrayVariableDefinition;
            _index = index;
            _parameter = parameterDefinition;
            _processor = methodProcessor;
            _creator = creator;
        }

        public TypeReference PersistedType => _parameter.ParameterType;

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
                if (_passOriginalReference)
                {
                    // the value is written into the variable of the caller, which the called method gets: the method works
                    // on the variable itself (a returned reference points to it, a value assigned before an exception stays)
                    instructions.Insert(0, _processor.Create(OpCodes.Ldarg, _parameter));
                    instructions.Add(castToType.GetStIndInstruction());
                    instructions.Add(_processor.Create(OpCodes.Ldarg, _parameter));
                }
                else
                {
                    // a read only parameter (in): the called method gets the address of a copy
                    var variable = _creator.CreateVariable(castToType).Variable;
                    instructions.Add(_processor.Create(OpCodes.Stloc, variable));
                    instructions.Add(_processor.Create(OpCodes.Ldloca, variable));
                }
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