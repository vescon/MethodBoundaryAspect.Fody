using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Records the names of the weaved methods, for filtering by <see cref="OnMethodBoundaryAspect.AttributeTargetMemberAttributes"/>.
    /// </summary>
    public class VisibilityRecordingAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            lock (Calls)
                Calls.Add(arg.Method.Name);
        }
    }
}
