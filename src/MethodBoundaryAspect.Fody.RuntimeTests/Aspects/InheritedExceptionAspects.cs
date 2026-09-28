using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    // https://github.com/vescon/MethodBoundaryAspect.Fody/issues/46
    // an aspect derived from another aspect, which overrides OnException and calls the base implementation

    /// <summary>
    /// Logs the exception and lets it propagate.
    /// </summary>
    public class LogExceptionAndThrowAttribute : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnException(MethodExecutionArgs arg)
        {
            Record($"{GetType().Name}.base {arg.Method.Name} {arg.Exception.Message}");
        }

        protected static void Record(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }

    /// <summary>
    /// Logs the exception via the base aspect and swallows it.
    /// </summary>
    public class LogExceptionAndSwallowAttribute : LogExceptionAndThrowAttribute
    {
        public override void OnException(MethodExecutionArgs arg)
        {
            base.OnException(arg);

            Record($"{GetType().Name}.derived {arg.Method.Name}");
            arg.ReturnValue = -1;
            arg.FlowBehavior = FlowBehavior.Continue;
        }
    }

    /// <summary>
    /// Only adds OnEntry, OnException is inherited from <see cref="LogExceptionAndThrowAttribute"/>.
    /// </summary>
    public class LogEntryAndExceptionAttribute : LogExceptionAndThrowAttribute
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            Record($"{GetType().Name}.OnEntry {arg.Method.Name}");
        }
    }
}
