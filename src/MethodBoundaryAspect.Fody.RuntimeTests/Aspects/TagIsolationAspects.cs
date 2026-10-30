using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public class TagSetterAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = "tag of the setter";
        }
    }

    /// <summary>
    /// Never sets the tag, so it must always see null.
    /// </summary>
    public class TagObserverAttribute : OnMethodBoundaryAspect
    {
        public static readonly List<string> Seen = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Record("OnEntry", arg);
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            Record("OnExit", arg);
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            Record("OnException", arg);
        }

        private static void Record(string method, MethodExecutionArgs arg)
        {
            lock (Seen)
                Seen.Add($"{method} {arg.MethodExecutionTag ?? "null"}");
        }
    }
}
