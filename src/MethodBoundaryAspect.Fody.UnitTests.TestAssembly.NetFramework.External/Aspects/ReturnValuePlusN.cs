using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.External.Aspects
{
    [ProvideAspectRole("Second")]
#pragma warning disable 0618
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, "First")]
#pragma warning restore 0618
    public class ReturnValuePlusN : OnMethodBoundaryAspect
    {
        int _n;

        public ReturnValuePlusN(int n)
        {
            _n = n;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            int retVal = (int)arg.ReturnValue;
            retVal += _n;
            arg.ReturnValue = retVal;
        }
    }
}