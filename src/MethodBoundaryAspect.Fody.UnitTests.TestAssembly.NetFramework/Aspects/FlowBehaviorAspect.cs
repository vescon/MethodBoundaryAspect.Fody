using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class FlowBehaviorAspect : OnMethodBoundaryAspect
    {
        public FlowBehavior Behavior { get; set; }
        public object ReturnValue { get; set; }
        
        public override void OnException(MethodExecutionArgs arg)
        {
            arg.FlowBehavior = Behavior;
            arg.ReturnValue = ReturnValue;
        }
    }
}
