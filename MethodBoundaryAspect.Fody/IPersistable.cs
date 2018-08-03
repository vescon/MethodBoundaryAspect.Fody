using Mono.Cecil;

namespace MethodBoundaryAspect.Fody
{
    public interface IPersistable : ILoadable
    {
        InstructionBlock Store(InstructionBlock loadNewValueOntoStack, TypeReference typeOnStack);
    }
}
