using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    public class MethodInfoCacheMethods
    {
        [MethodUsageAspect]
        public int NonGeneric(int value) => value;

        [MethodUsageAspect]
        public T GenericMethod<T>(T value) => value;
    }

    public class MethodInfoCacheGeneric<TClass>
    {
        [MethodUsageAspect]
        public TClass NonGenericMethod(TClass value) => value;

        [MethodUsageAspect]
        public TMethod GenericMethod<TMethod>(TClass value, TMethod other) => other;
    }
}
