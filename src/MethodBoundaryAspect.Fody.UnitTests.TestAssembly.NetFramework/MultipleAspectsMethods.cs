using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
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