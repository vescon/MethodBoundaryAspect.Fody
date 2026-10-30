using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public class SetIntReturnValueAttribute : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            arg.ReturnValue = 99;
        }
    }

    public class SetNullReturnValueAttribute : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            arg.ReturnValue = null;
        }
    }
}
