using System;
using System.Collections.Generic;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
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
