using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class SetNamedArgumentAspectMethods
    {
        public static object Result { get; set; }

        [SetNamedArgumentAspect(Value = 43, BoolValue = true, AllowedValue = AllowedValue.Value2, Field = "test", IntField = 10)]
        public static void StaticMethodCall()
        {
        }

        [SetNamedArgumentAspect(Value = 43, BoolValue = true, AllowedValue = AllowedValue.Value2, Field = "test", IntField = 10)]
        public void InstanceMethodCall()
        {
        }
    }
}