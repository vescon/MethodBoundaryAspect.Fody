using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Shared.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    [WeaveAllMethodsInAssembly]
    public class ClassWithDelegate
    {
        public delegate void SomeDelegate();
    }
}
