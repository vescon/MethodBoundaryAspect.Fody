using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.UnverifiableTestAssembly.NetFramework;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    /// <summary>
    /// Pointer arguments and return values were boxed or unboxed like other values,
    /// which is invalid IL ("Common Language Runtime detected an invalid program").
    /// </summary>
    public class PointerArgumentsTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private static readonly Type TestClassType = typeof(PointerMethods);

        [Fact]
        public void PointerArgumentShouldBePassedUnchangedAndBeNullInArguments()
        {
            WeaveAssemblyClassAndLoad(TestClassType);

            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), nameof(PointerMethods.CallDereference));

            result.Should().Be("42 [null, 2]");
        }

        [Fact]
        public void PointerReturnValueShouldBeReturnedAndBeNullInReturnValue()
        {
            WeaveAssemblyClassAndLoad(TestClassType);

            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), nameof(PointerMethods.CallIdentity));

            result.Should().Be("True null");
        }
    }
}
