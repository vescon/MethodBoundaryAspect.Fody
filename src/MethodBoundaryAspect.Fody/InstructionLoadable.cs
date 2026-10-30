using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// Loads a value with instructions which are created for each load (an instruction can only be used once).
    /// </summary>
    public class InstructionLoadable : ILoadable
    {
        private readonly Func<IEnumerable<Instruction>> _createInstructions;

        public InstructionLoadable(TypeReference persistedType, Func<IEnumerable<Instruction>> createInstructions)
        {
            PersistedType = persistedType;
            _createInstructions = createInstructions;
        }

        public TypeReference PersistedType { get; }

        public InstructionBlock Load(bool forDereferencing, bool onlyValue)
        {
            return new InstructionBlock("Load prepared value", new List<Instruction>(_createInstructions()));
        }
    }
}
