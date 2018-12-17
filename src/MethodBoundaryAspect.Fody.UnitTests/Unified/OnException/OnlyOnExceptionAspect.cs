using System.Diagnostics;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.Unified.OnException
{
    public class OnlyOnExceptionAspect : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs arg)
        {
            Debug.WriteLine("OnException called for: " + arg.Method.Name);
        }
    }
}