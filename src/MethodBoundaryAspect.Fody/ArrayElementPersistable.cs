using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// An element of an object array in a variable or field. Unlike the field of a state machine (a struct which is
    /// copied when the method awaits for the first time), the array is shared by all copies.
    /// </summary>
    public class ArrayElementPersistable : IPersistable
    {
        private readonly ILoadable _array;
        private readonly int _index;

        public ArrayElementPersistable(ILoadable array, int index, TypeReference elementType)
        {
            _array = array;
            _index = index;
            PersistedType = elementType;
        }

        public TypeReference PersistedType { get; }

        public InstructionBlock Load(bool forDereferencing, bool onlyValue)
        {
            var instructions = new List<Instruction>();
            instructions.AddRange(_array.Load(true, onlyValue).Instructions);
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4, _index));
            instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
            return new InstructionBlock("Load element", instructions);
        }

        public InstructionBlock Store(InstructionBlock loadNewValueOntoStack, TypeReference typeOnStack)
        {
            var instructions = new List<Instruction>();
            instructions.AddRange(_array.Load(true, false).Instructions);
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4, _index));
            instructions.AddRange(loadNewValueOntoStack.Instructions);
            instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            return new InstructionBlock("Store element", instructions);
        }
    }
}
