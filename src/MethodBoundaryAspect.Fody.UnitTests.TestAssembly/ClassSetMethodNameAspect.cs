using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    [ClassSetMethodNameAspectAspect]
    public class ClassSetMethodNameAspect
    {
        public static object Result { get; set; }

        public static void ClassStaticMethodCall()
        {
        }

        public void ClassInstanceMethodCall()
        {
        }
    }
}