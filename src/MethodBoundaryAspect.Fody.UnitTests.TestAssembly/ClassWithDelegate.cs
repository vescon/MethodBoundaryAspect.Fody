using MethodBoundaryAspect.Fody.UnitTests.TestAssemblyAspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    [WeaveAllMethodsInAssembly]
    public class ClassWithDelegate
    {
        public delegate void SomeDelegate();
    }
}
