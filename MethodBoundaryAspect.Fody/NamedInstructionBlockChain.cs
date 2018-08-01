using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    public class NamedInstructionBlockChain : InstructionBlockChain, IPersistable
    {
        public VariableDefinition Variable { get; }
        public TypeReference TypeReference { get; }

        VariablePersistable Persistable => new VariablePersistable(Variable);

        TypeReference ILoadable.PersistedType => Persistable.PersistedType;

        public NamedInstructionBlockChain(VariableDefinition variable, TypeReference typeReference)
        {
            Variable = variable;
            TypeReference = typeReference;
        }

        InstructionBlock ILoadable.Load(bool forDereferencing) => Persistable.Load(forDereferencing);

        InstructionBlock IPersistable.Store(InstructionBlock loadNewValueOntoStack, TypeReference typeOnStack) => Persistable.Store(loadNewValueOntoStack, typeOnStack);

        public InstructionBlock Flatten()
        {
            return new InstructionBlock("Flattened", InstructionBlocks.SelectMany(b => b.Instructions).ToList());
        }
    }
}