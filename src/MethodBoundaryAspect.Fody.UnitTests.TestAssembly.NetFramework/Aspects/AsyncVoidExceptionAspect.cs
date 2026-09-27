using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class AsyncVoidExceptionAspect : OnMethodBoundaryAspect
    {
        public bool SwallowException { get; set; }

        public override void OnException(MethodExecutionArgs arg)
        {
            AsyncVoidClass.Result += $"[OnException {arg.Method.Name}: {arg.Exception.Message}]";
            if (SwallowException)
                arg.FlowBehavior = FlowBehavior.Continue;
        }
    }
}
