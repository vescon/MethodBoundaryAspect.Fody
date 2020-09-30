using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody
{
    public class VariablePersistable : IPersistable
    {
        private VariableDefinition _def;

        public VariablePersistable(VariableDefinition def)
        {
            _def = def;
        }

        public TypeReference PersistedType => _def.VariableType;

        public InstructionBlock Load(bool forDereferencing, bool onlyValue)
        {
            var opCode = _def.VariableType.IsValueType && forDereferencing
                ? OpCodes.Ldloca 
                : OpCodes.Ldloc;
            return new InstructionBlock("Load", Instruction.Create(opCode, _def));
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
    }
}
