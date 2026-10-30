#if NETCOREAPP
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class PrivateTypeArgumentMethods
    {
        private class PrivateKey
        {
        }

        [TypeArgumentAspect<PrivateKey>]
        public void PrivateTypeArgument()
        {
        }
    }
}
#endif
