using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public class AsyncExceptionOnlyAspect : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs arg)
        {
            AsyncClass<Placeholder>.Result = "OnException third";
        }
    }
}
