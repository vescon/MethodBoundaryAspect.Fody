using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// A ref argument changed by an aspect ([AllowChangingInputArguments]) is the variable of the caller, not a copy
    /// which is written back after the call: the reference returned by a method with a ref return points into the
    /// variable of the caller (not into a local variable of the woven method), and a value assigned by the method
    /// before it throws is not lost.
    /// </summary>
    [Collection(nameof(ChangeFirstArgumentTo99Attribute))]
    public class ChangingRefArgumentTests
    {
        public ChangingRefArgumentTests()
        {
            lock (ChangeFirstArgumentTo99Attribute.ArgumentsInOnExit)
                ChangeFirstArgumentTo99Attribute.ArgumentsInOnExit.Clear();
        }

        [Fact]
        public void ReferenceReturnedByTheMethodShouldPointToTheVariableOfTheCaller()
        {
            var value = 1;

            ref var returned = ref new ChangingRefArgumentMethods().Pick(ref value);

            returned.Should().Be(99);
            value.Should().Be(99);
            returned = 5;
            value.Should().Be(5);
        }

        [Fact]
        public void ChangedRefArgumentShouldBePassedAndWrittenBack()
        {
            var value = 1;

            new ChangingRefArgumentMethods().Increment(ref value);

            value.Should().Be(100);
            ChangeFirstArgumentTo99Attribute.ArgumentsInOnExit.Should().Equal("100");
        }

        [Fact]
        public void OutArgumentShouldBeAssignedByTheMethod()
        {
            new ChangingRefArgumentMethods().Assign(out var value);

            value.Should().Be(7);
            ChangeFirstArgumentTo99Attribute.ArgumentsInOnExit.Should().Equal("7");
        }

        [Fact]
        public void RefValueAssignedBeforeAnExceptionShouldNotBeLost()
        {
            var value = 1;

            new ChangingRefArgumentMethods().IncrementAndThrow(ref value);

            value.Should().Be(100);
        }
    }
}
