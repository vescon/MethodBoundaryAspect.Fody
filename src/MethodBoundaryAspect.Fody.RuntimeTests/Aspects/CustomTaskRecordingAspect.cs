using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Only uses OnEntry and OnExit, so it can be woven into async methods with any async method builder.
    /// </summary>
    public class CustomTaskRecordingAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            lock (Calls)
                Calls.Add($"OnEntry {arg.Method.Name}");
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            lock (Calls)
                Calls.Add($"OnExit {arg.Method.Name}");
        }
    }
}
