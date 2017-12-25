using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public class SetClassNameAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            ClassSetMethodNameAspectResult.Result =
                arg.Method == null
                    ? "No method info found"
                    : arg.Method.Name;
        }
    }
}