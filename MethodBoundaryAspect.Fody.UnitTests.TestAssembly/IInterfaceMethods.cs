using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    [SetClassNameAspect]
    public interface IInterfaceMethods
    {
        void DoSomething();
    }
}