using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssemblyAspects
{
    public sealed class WeaveAllMethodsInAssemblyAttribute : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs args)
        {
                args.FlowBehavior = FlowBehavior.RethrowException;
        }
    }
}
