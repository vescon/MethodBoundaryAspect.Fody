using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Records the first argument in OnEntry and OnExit. Does not change arguments (no [AllowChangingInputArguments]).
    /// </summary>
    public class RecordFirstArgumentAttribute : OnMethodBoundaryAspect
    {
        public static readonly List<string> InOnEntry = new List<string>();
        public static readonly List<string> InOnExit = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            lock (InOnEntry)
                InOnEntry.Add(arg.Arguments[0]?.ToString());
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            lock (InOnExit)
                InOnExit.Add(arg.Arguments[0]?.ToString());
        }
    }
}
