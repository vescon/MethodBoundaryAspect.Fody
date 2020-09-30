using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace MethodBoundaryAspect.Fody
{
    public class FieldPersistable : IPersistable
    {
        private readonly ILoadable _instance;

        public FieldPersistable(ILoadable instance, FieldReference field)
        {
            _instance = instance;
            Field = field;
        }

        public FieldReference Field { get; }
        public TypeReference PersistedType => Field.FieldType;

        public InstructionBlock Load(bool forDereferencing, bool onlyValue)
        {
            var instructions = _instance.Load(true, onlyValue).Instructions
                .Concat(new[] { Instruction.Create(OpCodes.Ldfld, Field) })
                .ToList();
            return new InstructionBlock("Load", instructions);
        }

        public InstructionBlock Store(InstructionBlock loadNewValueOntoStack, TypeReference typeOnStack)
        {
            var list = new List<Instruction>();
            list.AddRange(_instance.Load(true, false).Instructions);
            list.AddRange(loadNewValueOntoStack.Instructions);
            list.Add(Instruction.Create(OpCodes.Stfld, Field));
            return new InstructionBlock("Store", list);
        }
    }
}
