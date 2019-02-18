using System;
using System.Threading;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class ExitAfterExceptionAspect : OnMethodBoundaryAspect
    {
        public int ReturnValue { get; set; }

        public ExitAfterExceptionAspect(int onExitsAllowed = 0) => m_exitsAllowed = onExitsAllowed;

        public override void OnException(MethodExecutionArgs arg)
        {
            arg.FlowBehavior = FlowBehavior.Continue;
            if (ReturnValue != 0)
                arg.ReturnValue = ReturnValue;
        }

        private int m_exitsAllowed;

        public override void OnExit(MethodExecutionArgs arg)
        {
            if (Interlocked.Decrement(ref m_exitsAllowed) < 0)
                throw new Exception("Should not call OnExit for the same aspect that aborted the exception handling.");
        }
    }
}
