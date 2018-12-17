using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    [OnlyOnEntryAspect]
    public interface IInterfaceMethods
    {
        void DoSomething();
    }
}