using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    public class ExecutionArgsUsageMethods
    {
        [NoUsageAspect]
        public int NoUsage(int value) => value;

        [MethodUsageAspect]
        public int MethodUsage(int value) => value;

        [ArgumentsUsageAspect]
        public int ArgumentsUsage(int value) => value;

        [ReadReturnValueUsageAspect]
        public int ReadReturnValueUsage(int value) => value;

        [WriteReturnValueUsageAspect]
        public int WriteReturnValueUsage(int value) => value;

        [InstanceUsageAspect]
        public int InstanceUsage(int value) => value;

        [NoUsageAspect]
        [ArgumentsUsageAspect]
        [MethodUsageAspect]
        public int CombinedUsage(int value) => value;

        [ExternalMethodUsageAspect]
        public int EscapingUsage(int value) => value;

        [OptOutUsageAspect]
        public int OptOutUsage(int value) => value;
    }
}
