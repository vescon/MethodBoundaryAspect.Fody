using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class SetMethodExecutionTagValueAspectMethods
    {
        public static object Result { get; set; }

        [SetMethodExecutionTagValueAspect]
        public static void StaticMethodCall()
        {
        }

        [SetMethodExecutionTagValueAspect]
        public void InstanceMethodCall()
        {
        }
    }
}