using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.Benchmark
{
    public class InvocationBenchmark
    {
        [Benchmark]
        public int MethodCallWithAspect() => TestClass.ExecuteWithAspect(5);

        [Benchmark]
        public int MethodCallWithoutAspect() => TestClass.ExecuteWithoutAspect(5);
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<InvocationBenchmark>();
        }
    }

    
    class TestClass
    {
        [TestAspect]
        public static int ExecuteWithAspect(int x)
        {
            var sum = 0;
            for (var i = 0; i < 100; i++) 
                sum += i * x;

            return sum;
        }
        
        public static int ExecuteWithoutAspect(int x)
        {
            var sum = 0;
            for (var i = 0; i < 100; i++) 
                sum += i * x;

            return sum;
        }
    }

    class TestAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            base.OnEntry(arg);
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            base.OnEntry(arg);
        }
    }
}
