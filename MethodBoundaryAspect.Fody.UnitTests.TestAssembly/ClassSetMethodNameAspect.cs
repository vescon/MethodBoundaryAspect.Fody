using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    [SetClassNameAspect]
    public class ClassSetMethodNameAspect
    {
        public static void ClassStaticMethodCall()
        {
        }

        public void ClassInstanceMethodCall()
        {
        }
    }
}