using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// An in argument changed by an aspect with [AllowChangingInputArguments] is passed to the method, but it was
    /// also written back to the variable of the caller, which is read only for the method (e.g. a readonly field).
    /// </summary>
    [Collection(nameof(NativeValueArgumentsAspect))]
    public class ReadOnlyParameterTests
    {
        public ReadOnlyParameterTests()
        {
            NativeValueArgumentsAspect.Arguments.Clear();
            NativeValueArgumentsAspect.Replacement = new IntPtrReplacement { Index = 0, Value = 20 };
        }

        [Fact]
        public void ChangedInArgumentShouldBePassedButNotWrittenBack()
        {
            var value = 10;

            new ReadOnlyParameterMethods().ReadIn(in value).Should().Be(20);

            value.Should().Be(10);
        }

        [Fact]
        public void ChangedInArgumentOfVirtualMethodShouldBePassedButNotWrittenBack()
        {
            var value = 10;

            new ReadOnlyParameterMethods().ReadInVirtual(in value).Should().Be(20);

            value.Should().Be(10);
        }

        [Fact]
        public void ReadonlyFieldPassedAsInArgumentShouldNotBeChanged()
        {
            var target = new ReadOnlyParameterMethods();

            target.ReadField().Should().Be(20);

            target.Field.Should().Be(10);
        }
    }
}
