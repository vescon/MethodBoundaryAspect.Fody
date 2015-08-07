using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class SetAspectNameResultAspectMethods
    {
        public static object Result { get; set; }

        [SetAspectNameResultAspect]
        public static void StaticMethodCall()
        {
        }

        [SetAspectNameResultAspect]
        public void InstanceMethodCall()
        {
        }
    }
}
