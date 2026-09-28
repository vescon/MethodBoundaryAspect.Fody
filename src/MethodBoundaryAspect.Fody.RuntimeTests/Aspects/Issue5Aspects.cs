using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public class WrappedException : Exception
    {
        public WrappedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Wraps the exception of the method by throwing a new exception in OnException.
    /// </summary>
    public class Issue5WrappingAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnException(MethodExecutionArgs arg)
        {
            Record($"OnException {nameof(Issue5WrappingAspect)} {arg.Exception.Message}");
            throw new WrappedException("Wrapped", arg.Exception);
        }

        public static void Record(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }

    public class Issue5RecordingAspect : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs arg)
        {
            Issue5WrappingAspect.Record($"OnException {nameof(Issue5RecordingAspect)} {arg.Exception.Message}");
        }
    }
}
