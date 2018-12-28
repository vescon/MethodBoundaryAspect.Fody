using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class SetAspectNameResultAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            SetAspectNameResultAspectMethods.Result = "SetAspectNameResultAspect";
        }
    }
}