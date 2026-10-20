using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Records the names of the woven methods in OnEntry. Tests using it belong to the collection
    /// <c>[Collection(nameof(NameRecordingAspect))]</c> and clear <see cref="Calls"/> before each test.
    /// </summary>
    public class NameRecordingAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            lock (Calls)
                Calls.Add(arg.Method.Name);
        }
    }
}
