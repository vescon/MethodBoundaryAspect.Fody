using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public class IteratorRecordingAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg) => Record($"OnEntry {arg.Method.Name}");

        public override void OnExit(MethodExecutionArgs arg) => Record($"OnExit {arg.Method.Name} returned {arg.ReturnValue?.GetType().Name}");

        public override void OnException(MethodExecutionArgs arg) => Record($"OnException {arg.Method.Name}");

        private static void Record(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }
}
