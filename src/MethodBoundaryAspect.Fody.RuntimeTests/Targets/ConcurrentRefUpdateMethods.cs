using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class ConcurrentRefUpdateMethods
    {
        [WaitInOnEntry]
        public void KeepValue(ref int value)
        {
        }

        [WaitAndReplaceInOnEntry]
        public void Replaced(ref int value)
        {
        }
    }
}
