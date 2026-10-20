#define RUNTIMETESTS_DEFINED_IN_TARGETS

using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class ConditionalMethods
    {
        [NotDefinedCondition]
        public int Omitted() => 1;

        [DefinedCondition]
        public int Woven() => 2;
    }

    [NotDefinedCondition]
    public class OmittedClass
    {
        public int Method() => 3;
    }
}
