using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class SetConstructorArgumentReferencesAspectMethods
    {
        public static object Result { get; set; }

        [SetConstructorArgumentReferencesAspect(typeof(string), "TestText")]
        public static void StaticMethodCall()
        {
        }

        [SetConstructorArgumentReferencesAspect(typeof(string), "TestText")]
        public void InstanceMethodCall()
        {
        }
    }
}