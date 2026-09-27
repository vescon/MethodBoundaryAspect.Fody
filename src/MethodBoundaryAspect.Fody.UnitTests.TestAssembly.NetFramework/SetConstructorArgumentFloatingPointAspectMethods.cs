using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class SetConstructorArgumentFloatingPointAspectMethods
    {
        public static object Result { get; set; }

        [SetConstructorArgumentFloatingPointAspect(4.9d, 1.5f, 'x', new[] { 0.25d, -3.5d }, 2.75d, NamedDoubleValue = 6.125d)]
        public static void StaticMethodCall()
        {
        }

        [SetConstructorArgumentFloatingPointAspect(4.9d, 1.5f, 'x', new[] { 0.25d, -3.5d }, 2.75d, NamedDoubleValue = 6.125d)]
        public void InstanceMethodCall()
        {
        }
    }
}
