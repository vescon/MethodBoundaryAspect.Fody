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

        public ArrayElementLoadable(
            VariableDefinition arrayVariableDefinition,
            int index,
            ParameterDefinition parameterDefinition,
            ILProcessor methodProcessor)
        {
            _arrayVariableDefinition = arrayVariableDefinition;
            _index = index;
            _parameter = parameterDefinition;
            _processor = methodProcessor;
        }

        public TypeReference PersistedType => _parameter.ParameterType;

        public InstructionBlock Load(bool forDereferencing)
        {
            var instructions = new List<Instruction>
            {
                _processor.Create(OpCodes.Ldloc, _arrayVariableDefinition),
                _processor.Create(OpCodes.Ldc_I4, _index),
                _processor.Create(OpCodes.Ldelem_Ref)
            };

            // using "_parameter.ParameterType" won't work for generic types
            // because in the method definition we only have the generic types, not the actual used one
            // so casting from object to e.g. "List<T>" makes no sense.
            // How do we get the closed generic type here for correct casting?
            // Reproduce via executing unit tests in class "GenericClassWithArity1Tests"
            var castOrUnbox = InstructionBlockCreator.CastValueCurrentlyOnStack(
                _arrayVariableDefinition.VariableType.GetElementType(), // object
                _parameter.ParameterType); // concrete type
            instructions.AddRange(castOrUnbox);

            return new InstructionBlock($"Load {_index}", instructions);
        }
    }
}