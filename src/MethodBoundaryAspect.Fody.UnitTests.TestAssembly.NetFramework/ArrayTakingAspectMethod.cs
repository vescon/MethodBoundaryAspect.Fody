using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class ArrayTakingAspectMethod
    {
        public static int[] Result { get; set; }

        [IntArrayTakingAspect(0, 42)]
        public static int[] Method()
        {
            return new int[0];
        }
    }
}