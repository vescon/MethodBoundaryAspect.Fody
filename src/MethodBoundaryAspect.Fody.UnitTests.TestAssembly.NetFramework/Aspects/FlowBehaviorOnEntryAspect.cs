using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class FlowBehaviorOnEntryAspect : OnMethodBoundaryAspect
    {
        public object ReturnValue { get; set; }
        public FlowBehavior FlowBehavior { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.FlowBehavior = FlowBehavior;
            arg.ReturnValue = ReturnValue;
        }
    }
}
