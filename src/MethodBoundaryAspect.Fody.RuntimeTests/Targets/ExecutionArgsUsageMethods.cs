using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class ExecutionArgsUsageMethods
    {
        [MethodNameAspect]
        public int Add(int a, int b) => a + b;

        [MethodNameAspect]
        public void RefAndOut(ref int value, out string text)
        {
            value++;
            text = "changed";
        }

        [MethodNameAspect]
        public int Throw() => throw new InvalidOperationException("An exception");

        [MethodNameAspect]
        public static T Generic<T>(T value) => value;

        [MethodNameAspect]
        public async Task<int> Async(int value)
        {
            await Task.Yield();
            return value * 2;
        }

        [ArgumentsAspect]
        public int Arguments(int a, string b) => a;

        [ReturnValueAspect]
        public int ReturnValue(int value) => value * 2;

        [OverwriteReturnValueAspect]
        public int OverwrittenReturnValue(int value) => value;

        [ConditionallyOverwriteReturnValueAspect]
        public int OverwriteConditionally(int value) => value;

        [ConditionallyOverwriteReturnValueAspect]
        public int KeepConditionally(int value) => value;

        [InstanceAspect]
        public int Instance() => 1;

        [MethodNameAspect]
        [ArgumentsAspect]
        [ReturnValueAspect]
        [InstanceAspect]
        public int Combined(int value) => value + 1;

        [SkipBodyAspect]
        public int SkippedInt() => throw new InvalidOperationException("Body should be skipped");

        [SkipBodyAspect]
        public string SkippedString() => throw new InvalidOperationException("Body should be skipped");

        [SwallowExceptionAspect]
        public int SwallowedInt() => throw new InvalidOperationException("An exception");

        [OptOutSkipBodyAspect]
        public int OptOutSkippedInt() => throw new InvalidOperationException("Body should be skipped");
    }

    public class ExecutionArgsUsageGeneric<T>
    {
        [MethodNameAspect]
        public TMethod OpenGeneric<TMethod>(T value, TMethod other) => other;
    }
}
