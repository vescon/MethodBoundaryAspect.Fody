using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class SetMethodNameAspectMethods
    {
        public static object Result { get; set; }

        [SetMethodNameAspect]
        public static void StaticMethodCall()
        {
        }

        [SetMethodNameAspect]
        public void InstanceMethodCall()
        {
        }
    }
}