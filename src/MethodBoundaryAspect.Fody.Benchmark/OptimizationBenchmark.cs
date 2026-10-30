using BenchmarkDotNet.Attributes;
using MethodBoundaryAspect.Fody.Attributes;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.Benchmark
{
    /// <summary>
    /// Cases for the code the weaver emits besides the basic call: several aspects, a method without parameters whose
    /// arguments are read, an async method without OnException.
    /// </summary>
    [MemoryDiagnoser]
    public class OptimizationBenchmark
    {
        [Benchmark(Baseline = true)]
        public int WithoutAspect() => OptimizationTargets.Plain(5);

        [Benchmark]
        public int TwoAspects() => OptimizationTargets.TwoAspects(5);

        [Benchmark]
        public int NoParametersArgumentsRead() => OptimizationTargets.NoParametersArgumentsRead();

        [Benchmark]
        public int AsyncWithoutAspect() => OptimizationTargets.AsyncPlain(5).GetAwaiter().GetResult();

        [Benchmark]
        public int AsyncOnEntryOnExit() => OptimizationTargets.AsyncWithAspect(5).GetAwaiter().GetResult();

        [Benchmark]
        public int AsyncTwoAspects() => OptimizationTargets.AsyncTwoAspects(5).GetAwaiter().GetResult();
    }

    static class OptimizationTargets
    {
        public static int Sum;

        public static int Plain(int x) => Work(x);

        [TestAspect, SecondAspect]
        public static int TwoAspects(int x) => Work(x);

        [ReadArgumentsAspect]
        public static int NoParametersArgumentsRead() => Work(1);

        public static async Task<int> AsyncPlain(int x)
        {
            await Task.CompletedTask;
            return Work(x);
        }

        [TestAspect]
        public static async Task<int> AsyncWithAspect(int x)
        {
            await Task.CompletedTask;
            return Work(x);
        }

        [TestAspect, SecondAspect]
        public static async Task<int> AsyncTwoAspects(int x)
        {
            await Task.CompletedTask;
            return Work(x);
        }

        private static int Work(int x)
        {
            for (var i = 0; i < 100; i++)
                Sum += x;
            return Sum;
        }
    }

    class SecondAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
        }
    }

    class ReadArgumentsAspect : OnMethodBoundaryAspect
    {
        public static int Count;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Count = arg.Arguments.Length;
        }
    }
}
