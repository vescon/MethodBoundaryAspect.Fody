using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public class SetAspectNameResultAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            SetAspectNameResultAspectMethods.Result = "SetAspectNameResultAspect";
        }
    }
}