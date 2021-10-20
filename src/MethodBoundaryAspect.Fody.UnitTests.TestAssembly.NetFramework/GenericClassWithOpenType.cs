using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class GenericClassWithOpenType
    {
        public static object Result { get; set; }

        [GenericClassWithOpenTypeSetReturnValueAspect]
        public static T OpenTypeMethod<T>(T arg)
        {
            return default;
        }

        public static void CallOpenTypeMethod(int value)
        {
            OpenTypeMethod(value);
        }
    }
}