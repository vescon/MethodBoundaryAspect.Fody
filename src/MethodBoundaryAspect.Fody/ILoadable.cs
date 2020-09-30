using Mono.Cecil;

namespace MethodBoundaryAspect.Fody
{
    public interface ILoadable
    {
        TypeReference PersistedType { get; }
        InstructionBlock Load(bool forDereferencing, bool onlyValue);
    }
}
