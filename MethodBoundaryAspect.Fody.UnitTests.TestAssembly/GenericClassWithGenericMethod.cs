using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class GenericClassWithGenericMethod<T1>
    {
        [SetInstanceValueAspect]
        public void DoIt<T2>()
        {
        }

        public void DoItNotWeaved<T2>()
        {
        }

        public void CallOther<T2>()
        {
            var thisRef = this;
            DoIt<T2>();
        }
    }
}