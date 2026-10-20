using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    // one aspect of the class for many methods, its type is checked once
    [OnlyOnEntryAspect]
    public class ManyMethodsOfOneAspect
    {
        public void One()
        {
        }

        public void Two()
        {
        }

        public int Three() => 3;

        public string Four(string text) => text;
    }

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
