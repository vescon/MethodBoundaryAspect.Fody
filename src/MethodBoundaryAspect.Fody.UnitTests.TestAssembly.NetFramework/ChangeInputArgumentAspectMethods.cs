using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class ChangeInputArgumentAspectMethods
    {
        public static object Result { get; set; }

        [ChangeInputArgumentAspect(Index = 0, Value = 42)]
        public static void StaticMethodCallFirstArgument(object arg1)
        {
            Result = arg1;
        }

        [ChangeInputArgumentAspect(Index = 1, Value = "42")]
        public void InstanceMethodCallSecondArgument(object arg1, object arg2)
        {
            Result = arg2;
        }
    }
}