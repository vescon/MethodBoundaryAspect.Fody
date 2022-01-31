using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class DuplicateEntryMethodsAspectMethods
    {
        public static object Result { get; set; }

        [DuplicateEntryMethodsAspect]
        public static void StaticMethodCall()
        {
        }

        [DuplicateEntryMethodsAspect]
        public void InstanceMethodCall()
        {
        }
    }
}