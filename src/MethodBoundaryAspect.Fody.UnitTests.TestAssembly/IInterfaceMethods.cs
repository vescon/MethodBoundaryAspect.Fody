using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    [OnlyOnEntryAspect]
    public interface IInterfaceMethods
    {
        void DoSomething();
    }
}