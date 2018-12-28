using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class SetMethodExecutionTagValueOnExitAspectMethods
    {
        public static object Result { get; set; }

        [SetMethodExecutionTagValueOnExitAspect1]
        [SetMethodExecutionTagValueOnExitAspect2]
        public static void StaticMethodCall()
        {
        }

        [SetMethodExecutionTagValueOnExitAspect1]
        [SetMethodExecutionTagValueOnExitAspect2]
        public void InstanceMethodCall()
        {
        }
    }
}