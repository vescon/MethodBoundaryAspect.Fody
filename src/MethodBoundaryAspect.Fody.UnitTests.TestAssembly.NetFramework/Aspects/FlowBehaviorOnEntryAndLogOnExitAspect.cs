using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class FlowBehaviorOnEntryAndLogOnExitAspect : OnMethodBoundaryAspect
    {
        public object ReturnValue { get; set; }

        public override void OnExit(MethodExecutionArgs arg)
        {
            arg.ReturnValue = ReturnValue;
        }
    }
}
