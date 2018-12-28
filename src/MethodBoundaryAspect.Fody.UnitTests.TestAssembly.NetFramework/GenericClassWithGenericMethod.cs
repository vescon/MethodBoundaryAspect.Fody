using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class GenericClassWithGenericMethod<T1>
    {
        [SetInstanceValueAspect]
        public void DoIt<T2>()
        {
        }

        [SetInstanceValueAspect]
        public T2 DoItWithReturn<T2>()
        {
            T2 t2 = default(T2);
            DoItNotWeaved<T2>(t2);
            return t2;
        }

        public void DoItNotWeaved<T2>(T2 t2)
        {
        }

        public void CallOther<T2>()
        {
            var thisRef = this;
            DoIt<T2>();
        }
    }
}