using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.UnverifiableTestAssembly.NetFramework.Aspects
{
    public class SetReturnValueAspect : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            SetReturnValueAspectMethods.Result = arg.ReturnValue;
        }
    }
}
