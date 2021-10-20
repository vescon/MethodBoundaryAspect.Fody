using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class SelfWeavingAspectMethods
    {
        public static object Result { get; set; }
        
        public static string StaticProperty { get; set; }
        
        [SelfWeavingAspect]
        public static string StaticMethodCall()
        {
            StaticProperty = "StaticValue";
            return StaticProperty;
        }
        
        [SelfWeavingAspect]
        public class SelfWeavingAspect : OnMethodBoundaryAspect
        {
            public override void OnEntry(MethodExecutionArgs arg)
            {
                SelfWeavingAspectMethods.Result += $"[{arg.Method.Name}]";
            }
        }
    }
}