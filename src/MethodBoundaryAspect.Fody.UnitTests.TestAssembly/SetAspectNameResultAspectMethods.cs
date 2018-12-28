using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
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
