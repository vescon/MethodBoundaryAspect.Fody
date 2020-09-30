using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects.Shared
{
    [AllowChangingInputArguments]
    public class ChangeFirstInputArgumentTo42IfNullAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            if (arg.Arguments[0] == null)
                arg.Arguments[0] = 42;
        }
    }
}