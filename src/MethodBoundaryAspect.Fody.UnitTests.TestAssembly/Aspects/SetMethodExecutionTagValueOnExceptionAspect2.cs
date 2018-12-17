using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public class SetMethodExecutionTagValueOnExceptionAspect2 : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = "MethodExecutionTag2";
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            SetMethodExecutionTagValueOnExceptionAspectMethods.Result = arg.MethodExecutionTag;
        }
    }
}