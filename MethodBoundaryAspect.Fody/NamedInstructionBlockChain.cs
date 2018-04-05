using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    public class NamedInstructionBlockChain : InstructionBlockChain
    {
        public VariableDefinition Variable { get; }
        public TypeReference TypeReference { get; }

        public NamedInstructionBlockChain(VariableDefinition variable, TypeReference typeReference)
        {
            Variable = variable;
            TypeReference = typeReference;
        }
    }
}