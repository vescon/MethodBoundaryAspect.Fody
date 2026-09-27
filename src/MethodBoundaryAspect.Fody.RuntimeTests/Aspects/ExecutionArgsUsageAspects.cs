using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    // Aspects which use only some MethodExecutionArgs properties, the others are not provided at runtime

    public static class ExecutionArgsUsageRecorder
    {
        public static readonly List<string> Calls = new List<string>();

        public static void Record(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }

    public class MethodNameAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => ExecutionArgsUsageRecorder.Record($"OnEntry {arg.Method.Name}");

        public override void OnExit(MethodExecutionArgs arg) => ExecutionArgsUsageRecorder.Record($"OnExit {arg.Method.Name}");

        public override void OnException(MethodExecutionArgs arg) => ExecutionArgsUsageRecorder.Record($"OnException {arg.Method.Name} {arg.Exception.Message}");
    }

    public class ArgumentsAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) =>
            ExecutionArgsUsageRecorder.Record($"Arguments [{string.Join(", ", arg.Arguments.Select(a => a ?? "null"))}]");
    }

    public class ReturnValueAspect : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg) => ExecutionArgsUsageRecorder.Record($"Returned {arg.ReturnValue}");
    }

    public class OverwriteReturnValueAspect : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg) => arg.ReturnValue = 42;
    }

    /// <summary>
    /// Overwrites the return value only for some methods, without reading it.
    /// </summary>
    public class ConditionallyOverwriteReturnValueAspect : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            if (arg.Method.Name.StartsWith("Overwrite"))
                arg.ReturnValue = 42;
        }
    }

    public class InstanceAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => ExecutionArgsUsageRecorder.Record($"Instance {arg.Instance?.GetType().Name ?? "null"}");
    }

    /// <summary>
    /// Skips the method body without setting a return value.
    /// </summary>
    public class SkipBodyAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => arg.FlowBehavior = FlowBehavior.Return;
    }

    /// <summary>
    /// Swallows exceptions without setting a return value.
    /// </summary>
    public class SwallowExceptionAspect : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs arg) => arg.FlowBehavior = FlowBehavior.Continue;
    }

    /// <summary>
    /// Like <see cref="SkipBodyAspect"/>, but the optimization is disabled for it in FodyWeavers.xml.
    /// </summary>
    public class OptOutSkipBodyAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => arg.FlowBehavior = FlowBehavior.Return;
    }
}
