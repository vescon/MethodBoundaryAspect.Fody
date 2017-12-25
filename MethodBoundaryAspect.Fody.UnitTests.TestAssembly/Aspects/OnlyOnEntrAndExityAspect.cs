using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public class OnlyOnEntrAndExityAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
        }
    }
}