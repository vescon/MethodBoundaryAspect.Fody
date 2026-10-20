using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    public class VarArgMethods
    {
        [OnlyOnEntryAspect]
        public int VarArg(int first, __arglist) => first;

        [OnlyOnEntryAspect]
        public int Normal(int first) => first;

        [NoAspectMethodsAspect]
        public int VarArgWithoutAspectMethods(int first, __arglist) => first;
    }
}
