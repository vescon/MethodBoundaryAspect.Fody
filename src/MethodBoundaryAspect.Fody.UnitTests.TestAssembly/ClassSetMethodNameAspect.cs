using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
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