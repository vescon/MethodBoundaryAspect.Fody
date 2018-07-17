using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MethodBoundaryAspect.Fody
{
    public class FieldPersistable : IPersistable
    {
        ILoadable _instance;
        public FieldReference Field { get; private set; }

        public FieldPersistable(ILoadable instance, FieldReference field)
        {
            _instance = instance;
            Field = field;
        }

        public TypeReference PersistedType { get => Field.FieldType; }

        public InstructionBlock Load(bool forDereferencing)
        {
            return new InstructionBlock("Load",
                _instance.Load(true).Instructions.Concat(
                    new[] { Instruction.Create(OpCodes.Ldfld, Field) }).ToList());
        }

        public InstructionBlock Store(InstructionBlock loadNewValueOntoStack)
        {
            var list = new List<Instruction>();
            list.AddRange(_instance.Load(true).Instructions);
            list.AddRange(loadNewValueOntoStack.Instructions);
            list.Add(Instruction.Create(OpCodes.Stfld, Field));
            return new InstructionBlock("Store", list);
        }
    }
}
