using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects
{
    public class OnlyOnEntryAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
        }
    }
}