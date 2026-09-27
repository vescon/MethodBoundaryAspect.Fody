using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class InheritedAspectMethods
    {
        public static string Result { get; set; }

        [InheritedAspect]
        public static void StaticMethodCall()
        {
        }

        [InheritedAspect]
        public void InstanceMethodCall()
        {
        }

        [InheritedAspect]
        public static int MethodWithReturnValue(int value)
        {
            return value * 2;
        }

        [InheritedOnExitAspect]
        public static void MethodWithAspectHooksOnDifferentInheritanceLevels()
        {
        }
    }
}
