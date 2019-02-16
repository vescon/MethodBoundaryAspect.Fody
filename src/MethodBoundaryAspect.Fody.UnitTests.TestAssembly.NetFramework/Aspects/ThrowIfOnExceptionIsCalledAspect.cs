using System;
using System.Threading;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class ThrowIfOnExceptionIsCalledAspect : OnMethodBoundaryAspect
    {
        int m_exitsAllowed;

        public int ReturnValue { get; set; }

        public ThrowIfOnExceptionIsCalledAspect(int exitsAllowed) => m_exitsAllowed = exitsAllowed;

        public override void OnEntry(MethodExecutionArgs arg) { }

        public override void OnException(MethodExecutionArgs arg)
        {
            throw new Exception("Should not get to OnException.");
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            if (Interlocked.Decrement(ref m_exitsAllowed) < 0)
                throw new Exception("Should not get to OnExit.");
            else if (ReturnValue != 0)
                arg.ReturnValue = ReturnValue;
        }
    }
}
