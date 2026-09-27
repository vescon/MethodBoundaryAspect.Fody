using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    [AllowChangingInputArguments]
    public class CaptureArgumentsOnExitAspect : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            ChangeInputArgumentAspectMethods.Result = arg.Arguments.Clone();
        }
    }
}
