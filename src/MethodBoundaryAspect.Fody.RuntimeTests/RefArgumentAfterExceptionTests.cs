using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// A method writes a ref argument into the variable of the caller before it throws: OnException sees the value in
    /// Arguments, not the one from before the call.
    /// </summary>
    public class RefArgumentAfterExceptionTests
    {
        [Fact]
        public void ArgumentsShouldContainTheRefValueInOnException()
        {
            lock (RecordRefArgumentsAttribute.Seen)
                RecordRefArgumentsAttribute.Seen.Clear();
            var value = 1;

            new RefArgumentAfterExceptionMethods().AssignAndThrow(ref value);

            value.Should().Be(5);
            lock (RecordRefArgumentsAttribute.Seen)
                RecordRefArgumentsAttribute.Seen.Should().Equal("exception:5");
        }
    }
}
