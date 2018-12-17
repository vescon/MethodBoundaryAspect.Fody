using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    [FirstAspect]
    [SecondAspect]
    public class MultipleAspectsMethods
    {
        public static string Result { get; set; }

        public static void StaticMethodCall()
        {
        }

        public void InstanceMethodCall()
        {
        }
    }
}