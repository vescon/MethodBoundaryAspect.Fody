using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Aspects
{
    public class RecordingMethodBoundaryAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            var arguments = string.Join(", ", arg.Arguments.Select(a => a ?? "null"));
            Record($"OnEntry {arg.Method.Name} [{arguments}] instance: {arg.Instance?.GetType().Name ?? "null"}");
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            Record($"OnExit {arg.Method.Name} returned {arg.ReturnValue ?? "null"}");
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            Record($"OnException {arg.Method.Name} {arg.Exception.Message}");
        }

        private static void Record(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }

    /// <summary>
    /// Changes int arguments, overwrites the return value and skips the method body.
    /// </summary>
    [AllowChangingInputArguments]
    public class OverwritingMethodBoundaryAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            for (var i = 0; i < arg.Arguments.Length; i++)
            {
                if (arg.Arguments[i] is int value)
                    arg.Arguments[i] = value * 10;
            }

            if (arg.Method.Name.StartsWith("Skip"))
            {
                arg.ReturnValue = new object();
                arg.FlowBehavior = FlowBehavior.Return;
            }
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            if (arg.Method.Name.StartsWith("Overwrite"))
                arg.ReturnValue = new object();
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            arg.ReturnValue = new object();
            arg.FlowBehavior = FlowBehavior.Continue;
        }
    }
}
