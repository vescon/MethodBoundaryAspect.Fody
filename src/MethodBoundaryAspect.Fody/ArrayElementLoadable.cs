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

            var parameterType = _parameter.ParameterType;
            var castToType = parameterType;
            if (parameterType.IsByReference) // support for ref-Arguments
                castToType = ((ByReferenceType) castToType).ElementType;

            var castOrUnbox = InstructionBlockCreator.CastValueCurrentlyOnStack(
                castFromType,
                castToType);
            instructions.AddRange(castOrUnbox);

            if (!onlyValue && parameterType.IsByReference) // support for ref-Arguments
            {
                var variable = _creator.CreateVariable(castToType).Variable;
                instructions.Add(_processor.Create(OpCodes.Stloc, variable));
                instructions.Add(_processor.Create(OpCodes.Ldloca, variable));
            }

            return new InstructionBlock($"Load {_index}", instructions);
        }
    }
}