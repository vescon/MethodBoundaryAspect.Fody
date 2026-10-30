using System;
using System.Threading;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// The signal between the two aspects: the exception of the asynchronous work is handled while the caller is
    /// still in OnExit. Tests that use the aspects reset it first and belong to the same collection.
    /// </summary>
    public static class AsyncReturnValueRaceSignal
    {
        public static readonly ManualResetEventSlim ExceptionHandled = new ManualResetEventSlim(false);

        public static volatile bool WaitedForException;
    }

    /// <summary>
    /// Keeps the caller in the woven method (in OnExit) until the asynchronous work has failed on another thread
    /// and its exception is handled, so the work always fails before the method returns its task.
    /// </summary>
    public class SlowOnExitAttribute : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            AsyncReturnValueRaceSignal.WaitedForException = AsyncReturnValueRaceSignal.ExceptionHandled.Wait(TimeSpan.FromSeconds(10));
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
            AsyncReturnValueRaceSignal.ExceptionHandled.Set();
        }
    }
}
