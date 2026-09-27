using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    [AllowChangingInputArguments]
    public class SetFirstOutArgumentAndReturnAspect : OnMethodBoundaryAspect
    {
        public int Value { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.Arguments[0] = Value;
            arg.ReturnValue = true;
            arg.FlowBehavior = FlowBehavior.Return;
        }
    }
}
