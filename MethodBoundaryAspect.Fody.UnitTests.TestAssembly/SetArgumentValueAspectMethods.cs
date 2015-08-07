using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class SetArgumentValueAspectMethods
    {
        public static object Result { get; set; }

        [SetArgumentValueAspect]
        public static void StaticMethodCall_ValueType(int i1)
        {
        }

        [SetArgumentValueAspect]
        public void InstanceMethodCall_ValueType(int i1)
        {
        }

        [SetArgumentValueAspect]
        public static void StaticMethodCall_ReferenceType(string s1)
        {
        }

        [SetArgumentValueAspect]
        public void InstanceMethodCall_ReferenceType(string s1)
        {
        }
    }
}