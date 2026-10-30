using System.Threading;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Keeps the caller in the woven method after OnEntry..OnExit started, so that the asynchronous work can fail on
    /// another thread before the method returns its task.
    /// </summary>
    public class SlowOnExitAttribute : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            Thread.Sleep(200);
        }
    }

    /// <summary>
    /// Swallows the exception of the asynchronous work, the task completes with the result -1.
    /// </summary>
    public class SwallowWithResultAttribute : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs arg)
        {
            arg.ReturnValue = -1;
            arg.FlowBehavior = FlowBehavior.Continue;
        }
    }
}
