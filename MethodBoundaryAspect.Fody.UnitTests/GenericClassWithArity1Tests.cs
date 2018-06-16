using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class GenericClassWithArity1Tests : GenericTestBase
    {
        public static IEnumerable<object[]> Methods { get => GetMethodNames(typeof(Generic<IDisposable>)); }

        public GenericClassWithArity1Tests()
        {
            OpenClassType = typeof(Generic<>);
            ClosedClassType = OpenClassType.MakeGenericType(typeof(Disposable));
        }

        [Theory, MemberData(nameof(Methods))]
        public void IfMethodInGenericClassIsWeaved_ThenResultIsExpected(string methodName)
        {
            RunTest(methodName);
        }
    }
}
