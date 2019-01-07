using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class SetMethodExecutionTagValueOnExitAspect1 : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = "MethodExecutionTag1";
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            SetMethodExecutionTagValueOnExitAspectMethods.Result = arg.MethodExecutionTag;
        }
    }
}