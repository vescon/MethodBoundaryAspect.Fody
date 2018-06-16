using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using System.Collections.Generic;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class GenericNestedClassTests : GenericTestBase
    {
        public static IEnumerable<object[]> Methods { get => GetMethodNames(typeof(Generic<List<Disposable[]>, Disposable>.Inner<List<List<Disposable[]>>>)); }

        public GenericNestedClassTests()
        {
            OpenClassType = typeof(Generic<,>.Inner<>);
            ClosedClassType = OpenClassType.MakeGenericType(typeof(List<Disposable[]>), typeof(Disposable), typeof(List<List<Disposable[]>>));
        }

        [Theory, MemberData(nameof(Methods))]
        public void IfMethodInNestedGenericClassIsWeaved_ThenResultIsExpected(string methodName)
        {
            RunTest(methodName);
        }
    }
}
