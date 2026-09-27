using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.Benchmark
{
    [MemoryDiagnoser]
    public class InvocationBenchmark
    {
        [Benchmark(Baseline = true)]
        public int CallWithoutAspect() => TestClass.ExecuteWithoutAspect(5);

        [Benchmark]
        public int CallWithAspect() => TestClass.ExecuteWithAspect(5);

        [Benchmark]
        public int CallWithMethodNameAspect() => TestClass.ExecuteWithMethodNameAspect(5);

        [Benchmark]
        public int CallWithAllPropertiesAspect() => TestClass.ExecuteWithAllPropertiesAspect(5);

        [Benchmark]
        public object OpenGenericCallWithoutAspect() =>
            TestOpenGenericClass<int>.OpenGenericWithoutAspect(new object());

        [Benchmark]
        public object OpenGenericCallWithAspect() => TestOpenGenericClass<int>.OpenGenericWithAspect(new object());

        [Benchmark]
        public object OpenGenericCallWithAllPropertiesAspect() =>
            TestOpenGenericClass<int>.OpenGenericWithAllPropertiesAspect(new object());
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
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

        [MethodNameAspect]
        public static int ExecuteWithMethodNameAspect(int x)
        {
            return DoWork(x);
        }

        [AllPropertiesAspect]
        public static int ExecuteWithAllPropertiesAspect(int x)
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

        [AllPropertiesAspect]
        public static T OpenGenericWithAllPropertiesAspect<T>(T x)
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

    /// <summary>
    /// Uses no MethodExecutionArgs property.
    /// </summary>
    class TestAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
        }
    }

    /// <summary>
    /// Uses only MethodExecutionArgs.Method.
    /// </summary>
    class MethodNameAspect : OnMethodBoundaryAspect
    {
        public static int Length;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Length = arg.Method.Name.Length;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
        }
    }

    /// <summary>
    /// Uses no MethodExecutionArgs property, but the optimization is disabled for it in FodyWeavers.xml:
    /// all properties are provided like before the optimization.
    /// </summary>
    class AllPropertiesAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
        }
    }
}
