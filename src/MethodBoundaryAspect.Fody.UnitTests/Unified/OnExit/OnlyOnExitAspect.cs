using System.Diagnostics;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.Unified.OnExit
{
    public class OnlyOnExitAspect : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            Debug.WriteLine("OnExit called for: " + arg.Method.Name);
        }
    }
}