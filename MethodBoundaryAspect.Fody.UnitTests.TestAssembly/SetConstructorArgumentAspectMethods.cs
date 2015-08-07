using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class SetConstructorArgumentAspectMethods
    {
        public static object Result { get; set; }

        [SetConstructorArgumentAspect(42, AllowedValue.Value3)]
        public static void StaticMethodCall()
        {
        }

        [SetConstructorArgumentAspect(42, AllowedValue.Value3)]
        public void InstanceMethodCall()
        {
        }
    }
}