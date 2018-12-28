using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects
{
    public class OnlyOnExceptionAspect : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs arg)
        {
        }
    }
}