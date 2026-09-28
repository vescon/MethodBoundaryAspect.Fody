using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Records the calls of the aspects of <see cref="Targets.Issue70Methods"/>.
    /// Declared outside of the woven classes so recording is not woven itself.
    /// </summary>
    public static class Issue70Log
    {
        public static readonly List<string> Calls = new List<string>();

        public static void Record(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }
}
