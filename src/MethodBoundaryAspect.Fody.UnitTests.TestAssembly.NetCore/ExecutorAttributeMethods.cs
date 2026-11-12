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

        // the trimmer checks the annotations of the generic parameter, the parameters and the return value of the method
        // which calls the executor against the ones of the executor
        [OnlyOnEntryAspect]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        public Type GenericAnnotated<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
        {
            return typeof(T);
        }
    }
}
