using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Only overrides OnException, like most of the reproductions in
    /// https://github.com/vescon/MethodBoundaryAspect.Fody/issues/66
    /// </summary>
    public class Issue66OnExceptionAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnException(MethodExecutionArgs arg)
        {
            lock (Calls)
                Calls.Add($"OnException {arg.Method.Name} {arg.Exception.Message}");
        }
    }

    /// <summary>
    /// The timing aspect from the issue: stores a Stopwatch in the MethodExecutionTag
    /// and continues the returned task in OnExit.
    /// </summary>
    public class Issue66TimingAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = Stopwatch.StartNew();
            Record($"OnEntry {arg.Method.Name}");
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            var timer = (Stopwatch)arg.MethodExecutionTag;
            if (arg.ReturnValue is Task task)
                task.ContinueWith(_ => timer.Stop(), TaskContinuationOptions.ExecuteSynchronously);
            else
                timer.Stop();

            Record($"OnExit {arg.Method.Name}");
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            var timer = (Stopwatch)arg.MethodExecutionTag;
            timer.Stop();
            Record($"OnException {arg.Method.Name} {arg.Exception.Message}");
        }

        private static void Record(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }
}
