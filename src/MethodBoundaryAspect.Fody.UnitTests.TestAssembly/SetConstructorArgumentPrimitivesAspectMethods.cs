using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class SetConstructorArgumentPrimitivesAspectMethods
    {
        public static object Result { get; set; }

        [SetConstructorArgumentPrimitivesAspect(42, AllowedValue.Value3)]
        public static void StaticMethodCall()
        {
        }

        [SetConstructorArgumentPrimitivesAspect(42, AllowedValue.Value3)]
        public void InstanceMethodCall()
        {
        }
    }
}