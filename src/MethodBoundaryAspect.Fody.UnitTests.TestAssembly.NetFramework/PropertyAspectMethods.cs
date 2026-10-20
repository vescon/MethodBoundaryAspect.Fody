using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class PropertyAspectMethods
    {
        public static object Result { get; set; }

        [PropertyAspect]
        public static string StaticProperty { get; set; }

        [PropertyAspect]
        public static string InstanceProperty { get; set; }

        [PropertyAspect]
        public static string GetterOnlyProperty => "GetterOnly";

        public static string GetterOnlyCall() => GetterOnlyProperty;

        public class NestedClass
        {
            // the aspect records its calls in the Result of the outer class
            public static object Result => PropertyAspectMethods.Result;

            [PropertyAspect]
            public static string NestedProperty { get; set; }

            public static string NestedCall()
            {
                NestedProperty = "NestedValue";
                return NestedProperty;
            }
        }

        public static string StaticMethodCall()
        {
            StaticProperty = "StaticValue";
            return StaticProperty;
        }

        public string InstanceMethodCall()
        {
            InstanceProperty = "InstanceValue";
            return InstanceProperty;
        }
    }
}