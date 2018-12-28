using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    [OnlyOnEntryAspect]
    public class InterfaceMethods
    {
        void DoSomething()
        {
        }
    }
}