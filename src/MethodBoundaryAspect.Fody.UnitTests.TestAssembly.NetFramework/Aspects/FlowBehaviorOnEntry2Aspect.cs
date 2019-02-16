using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    /// <summary>
    /// An exact clone of FlowBehaviorOnEntry, but gets around the fact that
    /// you can't have multiple instances of the exact same attribute type
    /// on a given member.
    /// </summary>
    public class FlowBehaviorOnEntry2Aspect : OnMethodBoundaryAspect
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
