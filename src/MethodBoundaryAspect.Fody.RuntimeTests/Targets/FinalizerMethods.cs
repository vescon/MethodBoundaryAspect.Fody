using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    [FinalizerCount]
    public class FinalizerMethods
    {
        public void Method()
        {
        }

        ~FinalizerMethods()
        {
        }
    }
}
