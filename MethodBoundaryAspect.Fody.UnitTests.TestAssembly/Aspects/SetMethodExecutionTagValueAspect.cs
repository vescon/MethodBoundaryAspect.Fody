using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public class SetMethodExecutionTagValueAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = "MethodExecutionTag";
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            SetMethodExecutionTagValueAspectMethods.Result = arg.MethodExecutionTag;
        }
    }
}