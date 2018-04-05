using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
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