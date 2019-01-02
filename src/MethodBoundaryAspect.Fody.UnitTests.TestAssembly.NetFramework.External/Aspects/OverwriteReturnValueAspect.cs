using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.External.Aspects
{
    public class OverwriteReturnValueAspect : OnMethodBoundaryAspect
    {
        object _o;

        public OverwriteReturnValueAspect(object o)
        {
            _o = o;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            arg.ReturnValue = _o;
        }
    }
}