using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public static class GenericOrderLog
    {
        public static readonly List<string> Calls = new List<string>();

        public static void Add(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }

    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
    public class GenericOrderAttribute<T> : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => GenericOrderLog.Add("Generic<" + typeof(T).Name + ">");
    }

    public class PlainOrderAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => GenericOrderLog.Add("Plain");
    }
}
