using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    [FirstAspect]
    [SecondAspect]
    public class MultipleAspectsWithIgnoredMethods
    {
        public void InstanceMethodCall()
        {
        }

        [DisableWeaving]
        public void PublicMethodIgnoredFromWeaving()
        {
        }

        [DisableWeaving]
        private void PrivateMethodIgnoredFromWeaving()
        {
        }
    }
}