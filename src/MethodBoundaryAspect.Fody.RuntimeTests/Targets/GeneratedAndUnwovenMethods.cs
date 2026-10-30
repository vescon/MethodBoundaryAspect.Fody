using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    /// <summary>The members the compiler generates for a record are woven like other methods.</summary>
    [NameRecordingAspect]
    public record AspectRecord(int Value);

#if NETCOREAPP
    public interface IDefaultMethod
    {
        [NameRecordingAspect]
        void Hello()
        {
        }
    }

    public class DefaultMethodImplementation : IDefaultMethod
    {
    }
#endif

    /// <summary>Lambdas, local functions and static constructors are not woven, only the methods around them.</summary>
    [NameRecordingAspect]
    public class UnwovenMethods
    {
        private static readonly int StaticValue = Compute();

        public int WithLambdaAndLocalFunction()
        {
            Func<int> lambda = () => 1;
            int Local() => 2;
            return lambda() + Local();
        }

        public static int ReadStatic() => StaticValue;

        // the aspect is not woven into the aspect itself, so a static helper of another class is not called twice
        private static int Compute() => 5;
    }
}
