using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;
using System.Diagnostics;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// [Conditional] on the aspect class: the compiler leaves out the attribute where the symbol is not defined,
    /// so the weaver never sees the aspect. Tests using the aspects belong to the collection
    /// <c>[Collection(nameof(ConditionalAspects))]</c>.
    /// </summary>
    [Conditional("RUNTIMETESTS_NEVER_DEFINED")]
    public class NotDefinedConditionAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            ConditionalAspects.Calls.Add("NotDefined " + arg.Method.Name);
        }
    }

    [Conditional("RUNTIMETESTS_DEFINED_IN_TARGETS")]
    public class DefinedConditionAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            ConditionalAspects.Calls.Add("Defined " + arg.Method.Name);
        }
    }

    public static class ConditionalAspects
    {
        public static readonly List<string> Calls = new List<string>();
    }
}
