using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.External.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    [WeaveAllMethodsInAssembly]
    public class ClassWithDelegate
    {
        public delegate void SomeDelegate();
    }
}
