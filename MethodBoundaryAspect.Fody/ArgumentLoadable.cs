using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    public class ArgumentLoadable : ILoadable
    {
        int _index;
        ParameterDefinition _parameter;
        ILProcessor _processor;

        public ArgumentLoadable(int i, ParameterDefinition p, ILProcessor methodProcessor)
        {
            _index = i;
            _parameter = p;
            _processor = methodProcessor;
        }

        public TypeReference PersistedType => _parameter.ParameterType;

        public InstructionBlock Load(bool forDereferencing)
        {
            return new InstructionBlock($"Load {_index}", _processor.Create(OpCodes.Ldarg_S, (byte)_index));
        }
    }
}
