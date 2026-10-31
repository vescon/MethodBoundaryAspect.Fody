using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Records the first argument (an int) seen in OnException and OnExit and swallows the exception.
    /// </summary>
    public class RecordRefArgumentsAttribute : OnMethodBoundaryAspect
    {
        public static readonly List<string> Seen = new List<string>();

        public override void OnException(MethodExecutionArgs arg)
        {
            lock (Seen)
                Seen.Add("exception:" + arg.Arguments[0]);
            arg.FlowBehavior = FlowBehavior.Continue;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            lock (Seen)
                Seen.Add("exit:" + arg.Arguments[0]);
        }
    }
}
