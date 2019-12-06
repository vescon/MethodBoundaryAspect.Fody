using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.External.Aspects
{
    [ProvideAspectRole("First")]
#pragma warning disable 0618
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, "Second")]
#pragma warning restore 0618
    public class ReturnValueTimesN : OnMethodBoundaryAspect
    {
        int _n;

        public ReturnValueTimesN(int n)
        {
            _n = n;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            int retVal = (int)arg.ReturnValue;
            retVal *= _n;
            arg.ReturnValue = retVal;
        }
    }
}