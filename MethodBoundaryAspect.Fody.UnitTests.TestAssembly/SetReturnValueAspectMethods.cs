using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class SetReturnValueAspectMethods
    {
        public static object Result { get; set; }

        [SetReturnValueAspect]
        public static int StaticMethodCall_ValueType()
        {
            return 42;
        }

        [SetReturnValueAspect]
        public int InstanceMethodCall_ValueType()
        {
            return 43;
        }

        [SetReturnValueAspect]
        public static string StaticMethodCall_ReferenceType()
        {
            return "42";
        }

        [SetReturnValueAspect]
        public string InstanceMethodCall_ReferenceType()
        {
            return "43";
        }
    }
}