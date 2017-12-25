using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public class SetMethodNameAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            SetMethodNameAspectMethods.Result =
                arg.Method == null
                    ? "No method info found"
                    : arg.Method.Name;
        }
    }
}