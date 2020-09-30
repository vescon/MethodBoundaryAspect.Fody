using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    [FirstAspect]
    [SecondAspect]
    [DisableWeaving]
    public class MultipleAspectsWithIgnoredClass
    {
        public void InstanceMethodCall()
        {
        }
        
        public void PublicMethodIgnoredFromWeaving()
        {
        }

        private void PrivateMethodIgnoredFromWeaving()
        {
        }
    }
}