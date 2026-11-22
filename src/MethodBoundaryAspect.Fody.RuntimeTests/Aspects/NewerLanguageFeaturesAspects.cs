using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Records the calls with the arguments ("null" for values which cannot be boxed) and the return value
    /// (only numbers and strings, the task of an async method has no fixed text).
    /// </summary>
    public class FeatureRecordingAttribute : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            var arguments = string.Join(", ", arg.Arguments.Select(x => x?.ToString() ?? "null"));
            Add($"OnEntry {arg.Method.Name}({arguments})");
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            var returnValue = arg.ReturnValue is int || arg.ReturnValue is string ? " " + arg.ReturnValue : string.Empty;
            Add($"OnExit {arg.Method.Name}{returnValue}");
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            Add($"OnException {arg.Method.Name} {arg.Exception.Message}");
        }

        private static void Add(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }

    /// <summary>Swallows the exception of an async method, the task completes with the result -1.</summary>
    public class FeatureSwallowAttribute : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs arg)
        {
            arg.ReturnValue = -1;
            arg.FlowBehavior = FlowBehavior.Continue;
        }
    }
}
