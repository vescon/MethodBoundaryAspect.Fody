using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class OnlyOnExitAspect : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
        }
    }
}