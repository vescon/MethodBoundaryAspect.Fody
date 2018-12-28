using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
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