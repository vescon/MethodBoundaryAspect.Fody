using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
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