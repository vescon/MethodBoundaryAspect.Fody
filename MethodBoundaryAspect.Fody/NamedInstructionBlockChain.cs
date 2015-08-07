using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    public class NamedInstructionBlockChain : InstructionBlockChain
    {
        public VariableDefinition Variable { get; private set; }
        public TypeReference TypeReference { get; private set; }

        public NamedInstructionBlockChain(VariableDefinition variable, TypeReference typeReference)
        {
            Variable = variable;
            TypeReference = typeReference;
        }
    }
}