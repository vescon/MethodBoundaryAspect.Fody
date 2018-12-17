using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects
{
    public sealed class ExceptionAssemblyAttribute : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs args)
        {
            args.FlowBehavior = FlowBehavior.RethrowException;
        }
    }
}
