using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public struct CoverageStruct
    {
        public int Value;

        [CoverageRecordingAspect]
        public int Add(int x) => Value + x;

        [CoverageRecordingAspect]
        public void Increment() => Value++;
    }

    public class CoverageMethods
    {
        [CoverageSwallowAspect]
        public async void AsyncVoidThrow()
        {
            await Task.Yield();
            throw new InvalidOperationException("async void failed");
        }

        [CoverageReturnAspect(Result = 5)]
        public ValueTask<int> SkippedWithResult() => throw new InvalidOperationException("the body must be skipped");

        [CoverageReturnAspect]
        public ValueTask<int> SkippedWithoutResult() => throw new InvalidOperationException("the body must be skipped");

        [CoverageReturnAspect]
        public ValueTask Skipped() => throw new InvalidOperationException("the body must be skipped");

        [CoverageNoEffectAspect(Behavior = FlowBehavior.Continue)]
        public async ValueTask<int> ContinueInOnEntry()
        {
            await Task.Yield();
            return 7;
        }

        [CoverageNoEffectAspect(Behavior = FlowBehavior.RethrowException)]
        public async ValueTask<int> RethrowInOnEntry()
        {
            await Task.Yield();
            return 8;
        }

#if NETCOREAPP // the unverifiable code fails PEVerify, which runs when the project is built for .NET Framework
        [CoverageRecordingAspect]
        public unsafe int CallFunctionPointer(delegate*<int, int> function, int value) => function(value);

        public static int Twice(int value) => value * 2;
#endif

        [CoverageMethodAspect]
        public static int FirstCallOfASharedAspect(int value) => value;
    }
}
