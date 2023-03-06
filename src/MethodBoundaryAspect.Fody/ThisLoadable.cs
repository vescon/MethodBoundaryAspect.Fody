using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    public class ThisLoadable : ILoadable
    {
        public ThisLoadable(TypeReference declaringType)
        {
            PersistedType = declaringType;
        }

        public TypeReference PersistedType { get; private set; }

        public InstructionBlock Load(bool forDereferencing, bool onlyValue)
        {
            return new InstructionBlock("Load", Instruction.Create(OpCodes.Ldarg_0));
        }
    }
}
