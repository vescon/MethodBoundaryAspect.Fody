using MethodBoundaryAspect.Fody.Attributes;
using System.Threading;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public class FinalizerCountAttribute : OnMethodBoundaryAspect
    {
        public static int Entries;

        public override void OnEntry(MethodExecutionArgs arg) => Interlocked.Increment(ref Entries);
    }
}
