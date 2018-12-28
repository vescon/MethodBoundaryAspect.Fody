using System;
using System.Collections.Generic;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class GenericForeignAssemblyTests : GenericTestBase
    {
        public GenericForeignAssemblyTests()
        {
            OpenClassType = typeof(GenericReferencingForeignAssembly<>);
            ClosedClassType = OpenClassType.MakeGenericType(typeof(Disposable));
        }

        public static IEnumerable<object[]> Methods
        {
            get
            {
                yield return new object[] { "GetInnerMixed", typeof(List<Disposable>), typeof(MethodDisposable) };
                yield return new object[] { "GetInnerOuterMixed", typeof(List<MethodDisposable>), typeof(MethodDisposable) };
                yield return new object[] { "TakeInnerMixed", typeof(List<Disposable>), typeof(MethodDisposable) };
                yield return new object[] { "TakeInnerOuterMixed", typeof(List<MethodDisposable>), typeof(MethodDisposable) };
                yield return new object[] { "RefTakeInnerMixed", typeof(List<Disposable>), typeof(MethodDisposable) };
                yield return new object[] { "RefTakeInnerOuterMixed", typeof(List<MethodDisposable>), typeof(MethodDisposable) };
            }
        }

        [Theory, MemberData(nameof(Methods))]
        public void IfInnerMixedForeignGenericIsWeaved_ThenCorrectResultIsPassed(string methodName, Type tArg1, Type tArg2)
        {
            // Arrange
            SetClosedMethod(methodName, tArg1, tArg2);

            // Act
            Weave(methodName);
            object result = AssemblyLoader.InvokeGenericMethod(ClosedClassType.TypeInfo(), methodName, new Type[] { tArg1, tArg2 }, Args);

            // Assert
            var results = result as List<string>;
            results[0].Trim().Should().Be(ExpectedEntryString(methodName));
            results[1].Trim().Should().Be(ExpectedExitString(methodName));
            results.Count.Should().Be(2);
        }
    }
}
