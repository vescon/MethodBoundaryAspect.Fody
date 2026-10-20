using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System.Runtime.CompilerServices;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class ExecutorMethods
    {
        [NameRecordingAspect]
        [SkipLocalsInit]
        public int SkipLocalsInitMethod()
        {
            var sum = 0;
            for (var i = 0; i < 4; i++)
                sum += i;
            return sum;
        }

        [NameRecordingAspect]
        public int InitLocalsMethod()
        {
            var sum = 0;
            for (var i = 0; i < 4; i++)
                sum += i;
            return sum;
        }

        [NameRecordingAspect]
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public int NoOptimizationMethod() => 1;

        [NameRecordingAspect]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int NoInliningMethod() => 1;

        [NameRecordingAspect]
        public int PlainMethod() => 1;
    }
}
