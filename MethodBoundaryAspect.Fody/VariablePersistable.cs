using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;

namespace MethodBoundaryAspect.Fody
{
    public class VariablePersistable : IPersistable
    {
        VariableDefinition _def;

        public VariablePersistable(VariableDefinition def)
        {
            _def = def;
        }

        public InstructionBlock Load(bool forDereferencing)
        {
            return new InstructionBlock("Load", Instruction.Create(
                _def.VariableType.IsValueType && forDereferencing ? OpCodes.Ldloca : OpCodes.Ldloc, _def));
        }

        public InstructionBlock Store(InstructionBlock loadNewValueOntoStack)
        {
            return new InstructionBlock("Store",
                loadNewValueOntoStack.Instructions.Concat(new[] { Instruction.Create(OpCodes.Stloc, _def) }).ToList());
        }

        public TypeReference PersistedType { get => _def.VariableType; }
    }
}
