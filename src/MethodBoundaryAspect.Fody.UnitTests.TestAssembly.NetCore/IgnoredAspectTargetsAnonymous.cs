using System;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    // local functions and lambdas are never woven, the compiler accepts the aspect
    public class IgnoredAspectTargetsAnonymous
    {
        private int _field = 1;

        public int StaticLocalFunction()
        {
            [OnlyOnEntryAspect]
            static int Local(int x) => x + 1;

            return Local(1);
        }

        public int LocalFunctionUsingThis()
        {
            [OnlyOnEntryAspect]
            int LocalWithThis(int x) => x + _field;

            return LocalWithThis(1);
        }

        public int Lambda()
        {
            var lambda = [OnlyOnEntryAspect] (int x) => x + 1;
            return lambda(1);
        }

        public int LambdaWithCapture(int y)
        {
            Func<int, int> lambda = [OnlyOnEntryAspect] (int x) => x + y;
            return lambda(1);
        }

        public int WithoutAspect()
        {
            static int LocalWithoutAspect(int x) => x + 1;

            Func<int, int> lambda = x => x + 1;
            return lambda(LocalWithoutAspect(1));
        }
    }

    // an aspect of the class does not hit them and must not warn
    [OnlyOnEntryAspect]
    public class ClassAspectWithLocalFunction
    {
        public int Method()
        {
            static int Local(int x) => x + 1;

            return Local(1);
        }
    }
}
