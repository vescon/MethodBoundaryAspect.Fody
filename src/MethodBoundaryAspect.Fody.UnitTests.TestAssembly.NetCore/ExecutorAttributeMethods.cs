using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    public class ExecutorAttributeMethods
    {
        [OnlyOnEntryAspect]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [RequiresUnreferencedCode("Uses reflection")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test")]
        [Obsolete("Not copied")]
        public int Annotated(int value)
        {
            return value;
        }
    }
}
