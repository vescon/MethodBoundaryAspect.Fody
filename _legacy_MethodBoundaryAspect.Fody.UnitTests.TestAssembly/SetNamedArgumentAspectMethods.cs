using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class SetNamedArgumentAspectMethods
    {
        public static object Result { get; set; }

        [SetNamedArgumentAspect(Value = 43, BoolValue = true, AllowedValue = AllowedValue.Value2)]
        public static void StaticMethodCall()
        {
        }

        [SetNamedArgumentAspect(Value = 43, BoolValue = true, AllowedValue = AllowedValue.Value2)]
        public void InstanceMethodCall()
        {
        }
    }
}