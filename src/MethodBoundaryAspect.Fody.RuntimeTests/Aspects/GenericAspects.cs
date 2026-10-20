using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public static class GenericAspectLog
    {
        public static readonly List<string> Calls = new List<string>();

        public static void Add(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }

    /// <summary>
    /// A generic aspect (C# 11 generic attribute), applied e.g. as [GenericRecordingAspect&lt;int&gt;("prefix")].
    /// </summary>
    public class GenericRecordingAspect<T> : OnMethodBoundaryAspect
    {
        private readonly string _prefix;

        public string Label { get; set; }

        public int Number;

        public GenericRecordingAspect(string prefix)
        {
            _prefix = prefix;
        }

        public override void OnEntry(MethodExecutionArgs arg) =>
            GenericAspectLog.Add($"{_prefix} {typeof(T).Name} OnEntry {arg.Method.Name} Label={Label} Number={Number}");

        public override void OnExit(MethodExecutionArgs arg) =>
            GenericAspectLog.Add($"{_prefix} {typeof(T).Name} OnExit {arg.Method.Name} {arg.ReturnValue}");

        public override void OnException(MethodExecutionArgs arg) =>
            GenericAspectLog.Add($"{_prefix} {typeof(T).Name} OnException {arg.Method.Name} {arg.Exception.GetType().Name}");
    }

    public class GenericBaseAspect<T> : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) =>
            GenericAspectLog.Add($"{typeof(T).Name} OnEntry {arg.Method.Name}");
    }

    /// <summary>
    /// A non generic aspect derived from a generic base aspect.
    /// </summary>
    public class DerivedFromGenericAspect : GenericBaseAspect<string>
    {
    }
}
