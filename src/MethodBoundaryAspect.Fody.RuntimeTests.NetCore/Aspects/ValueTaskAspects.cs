using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Aspects
{
    public class ValueTaskRecordingAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Record($"OnEntry {arg.Method.Name}");
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            Record($"OnExit {arg.Method.Name}");
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            Record($"OnException {arg.Method.Name} {arg.Exception.Message}");
        }

        public static void Record(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }

    /// <summary>
    /// Swallows exceptions and replaces the result with 42 (if the method returns an int).
    /// </summary>
    public class ValueTaskSwallowingAspect : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs arg)
        {
            ValueTaskRecordingAspect.Record($"OnException {arg.Method.Name} {arg.Exception.Message}");
            if (arg.Method is System.Reflection.MethodInfo { ReturnType: var returnType } && returnType == typeof(System.Threading.Tasks.ValueTask<int>))
                arg.ReturnValue = 42;
            arg.FlowBehavior = FlowBehavior.Continue;
        }
    }
}
