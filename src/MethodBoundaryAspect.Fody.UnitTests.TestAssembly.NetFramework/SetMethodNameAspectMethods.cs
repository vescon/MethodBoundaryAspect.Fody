using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
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