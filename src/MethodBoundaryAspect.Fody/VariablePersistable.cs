using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
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

        public InstructionBlock Store(InstructionBlock loadNewValueOntoStack, TypeReference typeOnStack)
        {
            Instruction preInstruction = null;
            Instruction postInstruction;
            if (_def.VariableType.IsByReference && !typeOnStack.IsByReference)
            {
                preInstruction = Instruction.Create(OpCodes.Ldloc, _def);
                postInstruction = ((ByReferenceType)_def.VariableType).ElementType.GetStIndInstruction();
            }
            else
                postInstruction = Instruction.Create(OpCodes.Stloc, _def);

            var list = new List<Instruction>();
            if (preInstruction != null)
                list.Add(preInstruction);
            list.AddRange(loadNewValueOntoStack.Instructions);
            list.Add(postInstruction);
            return new InstructionBlock("Store", list);
        }

        public TypeReference PersistedType { get => _def.VariableType; }
    }
}
