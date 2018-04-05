using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class ClassWithGenericMethod
    {
        [SetInstanceValueAspect]
        public void DoIt<T>()
        {
        }

        public void DoItNotWeaved<T>()
        {
        }

        public void CallOther<T>()
        {
            var thisRef = this;
            DoIt<T>();
        }
    }
}