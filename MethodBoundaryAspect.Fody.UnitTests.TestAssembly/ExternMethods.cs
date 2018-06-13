using System;
using System.Runtime.InteropServices;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    [OnlyOnEntryAspect]
    public class ExternMethods
    {
        [Flags]
        public enum ExecutionState : uint
        {
            EsAwayModeRequired = 0x00000040,
            EsContinuous = 0x80000000,
            EsDisplayRequired = 0x00000002,
            EsSystemRequired = 0x00000001
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern ExecutionState SetThreadExecutionState(ExecutionState flags);

        public static void DoSomething()
        {
        }
    }
}