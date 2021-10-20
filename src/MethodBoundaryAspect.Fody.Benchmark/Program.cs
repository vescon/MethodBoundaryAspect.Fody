using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.Benchmark
{
    public class InvocationBenchmark
    {
        [Benchmark]
        public int CallWithoutAspect() => TestClass.ExecuteWithoutAspect(5);

        [Benchmark]
        public int CallWithAspect() => TestClass.ExecuteWithAspect(5);

        [Benchmark]
        public object OpenGenericCallWithoutAspect() =>
            TestOpenGenericClass<int>.OpenGenericWithoutAspect(new object());

        [Benchmark]
        public object OpenGenericCallWithAspect() => TestOpenGenericClass<int>.OpenGenericWithAspect(new object());
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
        public static int Sum;

        [TestAspect]
        public static int ExecuteWithAspect(int x)
        {
            return DoWork(x);
        }

        public static int ExecuteWithoutAspect(int x)
        {
            return DoWork(x);
        }

        private static int DoWork(int x)
        {
            for (var i = 0; i < 100; i++)
                Sum += x;
            return Sum;
        }
    }

    class TestOpenGenericClass<TOuter>
    {
        public static int Sum;

        [TestAspect]
        public static T OpenGenericWithAspect<T>(T x)
        {
            return DoWorkGeneric(x);
        }

        public static object OpenGenericWithoutAspect(object x)
        {
            return DoWorkGeneric(x);
        }

        public static T DoWorkGeneric<T>(T x)
        {
            for (var i = 0; i < 100; i++)
                Sum++;
            return default;
        }
    }

    class TestAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
        }
    }
}
