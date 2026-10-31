using System;
using System.Threading;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// The signals between the test and the aspects: the aspect waits in OnEntry until the test has changed the
    /// variable of the caller. Tests that use the aspects reset them first and belong to the same collection.
    /// </summary>
    public static class ConcurrentRefUpdateSignal
    {
        public static readonly ManualResetEventSlim InOnEntry = new ManualResetEventSlim(false);

        public static readonly ManualResetEventSlim Continue = new ManualResetEventSlim(false);

        public static void Reset()
        {
            InOnEntry.Reset();
            Continue.Reset();
        }
    }

    /// <summary>
    /// Allows changing the arguments, but doesn't change any.
    /// </summary>
    [AllowChangingInputArguments]
    public class WaitInOnEntryAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            ConcurrentRefUpdateSignal.InOnEntry.Set();
            ConcurrentRefUpdateSignal.Continue.Wait(TimeSpan.FromSeconds(10));
        }
    }

    /// <summary>
    /// Allows changing the arguments and changes the first one.
    /// </summary>
    [AllowChangingInputArguments]
    public class WaitAndReplaceInOnEntryAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            ConcurrentRefUpdateSignal.InOnEntry.Set();
            ConcurrentRefUpdateSignal.Continue.Wait(TimeSpan.FromSeconds(10));
            arg.Arguments[0] = 100;
        }
    }
}
