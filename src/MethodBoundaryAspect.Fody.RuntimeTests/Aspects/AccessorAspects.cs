using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Records the names of the woven accessors in OnEntry, tests using it clear <see cref="Calls"/> first
    /// and belong to the collection <c>[Collection(nameof(AccessorAspectAttribute))]</c>.
    /// </summary>
    public class AccessorAspectAttribute : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            lock (Calls)
                Calls.Add(arg.Method.Name);
        }
    }
}
