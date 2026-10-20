using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public static class NestedOrderLog
    {
        public static readonly List<string> Calls = new List<string>();

        public static void Add(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }

    public class NestedOrderFirstAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => NestedOrderLog.Add("First");
    }

    public class NestedOrderSecondAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => NestedOrderLog.Add("Second");
    }
}
