using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class AsyncVoidAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            AsyncVoidClass.Result += "[OnEntry]";
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            AsyncVoidClass.Result += "[OnExit]";
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            AsyncVoidClass.Result += $"[OnException: {arg.Exception.Message}]";
        }
    }
}
