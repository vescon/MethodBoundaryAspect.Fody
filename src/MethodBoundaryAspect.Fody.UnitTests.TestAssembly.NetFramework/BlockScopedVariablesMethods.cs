using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class BlockScopedVariablesMethods
    {
        // Every local is read more than once so the compiler keeps it (and its debug scope) in Release builds.
        [OnlyOnEntryAspect]
        public static int MethodWithBlockScopedVariables(int count)
        {
            var one = 1;
            var sum = one;
            for (var i = 0; i < count; i++)
            {
                var two = i * 2;
                sum += two;
                if (i % 2 == 0)
                {
                    var three = two + i;
                    sum += three;
                    sum += three * one;
                }

                sum += two * one;
            }

            return sum;
        }
    }
}
