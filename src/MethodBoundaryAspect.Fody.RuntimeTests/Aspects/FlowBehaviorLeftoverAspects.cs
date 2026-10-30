using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Sets FlowBehavior.Continue in OnEntry, where it has no effect, and records the FlowBehavior it finds in OnException.
    /// </summary>
    public class ContinueInEntryAttribute : OnMethodBoundaryAspect
    {
        public static readonly List<FlowBehavior> SeenInOnException = new List<FlowBehavior>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.FlowBehavior = FlowBehavior.Continue;
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            lock (SeenInOnException)
                SeenInOnException.Add(arg.FlowBehavior);
        }
    }
}
