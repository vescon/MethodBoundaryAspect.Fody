using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    /// <summary>
    /// The control of <see cref="MultipleAspectsWithIgnoredNestedClass"/>: the same class without [DisableWeaving].
    /// </summary>
    [FirstAspect]
    [SecondAspect]
    public class MultipleAspectsWithNestedClass
    {
        public class NestedClass
        {
            public void InstanceMethodCall()
            {
            }

            private void PrivateMethod()
            {
            }
        }
    }
}
