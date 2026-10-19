using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    public class ArgumentLoadable : ILoadable
    {
        private int _index;
        private ParameterDefinition _parameter;
        private ILProcessor _processor;

        public ArgumentLoadable(int i, ParameterDefinition p, ILProcessor methodProcessor)
        {
            _index = i;
            _parameter = p;
            _processor = methodProcessor;
        }

        public TypeReference PersistedType => _parameter.ParameterType;

        public InstructionBlock Load(bool forDereferencing, bool onlyValue)
        {
            // ldarg.s with the index as byte would load another argument from the 256th argument on
            return new InstructionBlock($"Load {_index}", _processor.Create(OpCodes.Ldarg, _parameter));
        }
    }
}
