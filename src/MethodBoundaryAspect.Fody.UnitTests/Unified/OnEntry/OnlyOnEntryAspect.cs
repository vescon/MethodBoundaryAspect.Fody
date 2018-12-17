using System.Diagnostics;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.Unified.OnEntry
{
    public class OnlyOnEntryAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            Debug.WriteLine("OnEntry called for: " + arg.Method.Name);
        }
    }
}