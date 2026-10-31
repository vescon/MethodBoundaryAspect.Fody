using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public static class ExternalCalls
    {
        public static readonly List<string> Entries = new List<string>();

        public static void Add(string call)
        {
            lock (Entries)
                Entries.Add(call);
        }

        public static void Clear()
        {
            lock (Entries)
                Entries.Clear();
        }

        public static string[] Snapshot()
        {
            lock (Entries)
                return Entries.ToArray();
        }
    }

    /// <summary>
    /// Holds no state, can be shared by all calls.
    /// </summary>
    public class ExternalStatelessAttribute : OnMethodBoundaryAspect
    {
        public string Name { get; set; }

        public override void OnEntry(MethodExecutionArgs arg) => ExternalCalls.Add($"entry {Name} {arg.Method.Name}");

        public override void OnExit(MethodExecutionArgs arg) => ExternalCalls.Add($"exit {Name} {arg.Method.Name}");
    }

    /// <summary>
    /// Holds state: a new instance for every call, so the count is always 1.
    /// </summary>
    public class ExternalCountingAttribute : OnMethodBoundaryAspect
    {
        private int _count;

        public override void OnEntry(MethodExecutionArgs arg) => _count++;

        public override void OnExit(MethodExecutionArgs arg) => ExternalCalls.Add($"count {_count}");
    }

    public class ExternalGenericAttribute<T> : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => ExternalCalls.Add($"generic {typeof(T).Name}");
    }

    /// <summary>
    /// The base class of an aspect which is derived in another assembly.
    /// </summary>
    public abstract class ExternalBaseAspect : OnMethodBoundaryAspect
    {
        protected abstract string Label { get; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = Label;
            ExternalCalls.Add($"base entry {Label}");
        }

        public override void OnExit(MethodExecutionArgs arg) => ExternalCalls.Add($"base exit {arg.MethodExecutionTag}");
    }
}
