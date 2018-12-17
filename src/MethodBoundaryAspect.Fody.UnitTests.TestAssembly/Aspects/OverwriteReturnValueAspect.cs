using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public class OverwriteReturnValueAspect : OnMethodBoundaryAspect
    {
        object _retval;

        public OverwriteReturnValueAspect(object retVal)
        {
            _retval = retVal;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            arg.ReturnValue = _retval;
        }
    }
}