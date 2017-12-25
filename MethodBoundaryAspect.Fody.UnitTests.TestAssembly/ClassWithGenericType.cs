using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class ClassWithGenericType<T>
    {
        [SetInstanceValueAspect]
        public void DoIt()
        {
        }

        public void DoItNotWeaved()
        {
        }
    }
}