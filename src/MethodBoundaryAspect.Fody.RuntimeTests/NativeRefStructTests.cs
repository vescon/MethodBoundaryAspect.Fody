using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// TypedReference and RuntimeArgumentHandle cannot be boxed, also on .NET Framework, where the compiler does not
    /// mark them as ref structs: boxing them made the build fail (PEVerify) and the method an invalid program.
    /// </summary>
    [Collection(nameof(ReadArgumentsAttribute))]
    public class NativeRefStructTests
    {
        [Fact]
        public void TypedReferenceShouldBePassedToTheMethod()
        {
            var value = 1;

            NativeRefStructMethods.Typed(__makeref(value), 5).Should().Be(5);
            ReadArgumentsAttribute.ArgumentCount.Should().Be(2);
        }

        [Fact]
        public void RuntimeArgumentHandleShouldBePassedToTheMethod()
        {
            NativeRefStructMethods.Handle(default, 6).Should().Be(6);
            ReadArgumentsAttribute.ArgumentCount.Should().Be(2);
        }

    }
}
