using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    [AllowChangingInputArguments]
    public class ChangingInputAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => arg.Arguments[0] = 42;
    }
}
