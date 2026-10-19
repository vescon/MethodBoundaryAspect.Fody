using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// With several aspects using OnException, the woven exception handler jumped with a short form
    /// leave instruction over the handling code of the other aspects. From 4 aspects on the distance
    /// no longer fitted into a signed byte and the method threw an InvalidProgramException.
    /// </summary>
    [Collection(nameof(ManyAspectsRecordingAspect))]
    public class ManyAspectsTests
    {
        public ManyAspectsTests()
        {
            ManyAspectsRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void MethodWithFourAspectsShouldReturnResult()
        {
            new ManyAspectsMethods().AddOne(1).Should().Be(2);

            ManyAspectsRecordingAspect.Calls.Should().Equal(
                "OnEntry ManyAspectsA",
                "OnEntry ManyAspectsB",
                "OnEntry ManyAspectsC",
                "OnEntry ManyAspectsReturningD",
                "OnExit ManyAspectsReturningD ManyAspectsReturningD 2",
                "OnExit ManyAspectsC ManyAspectsC 2",
                "OnExit ManyAspectsB ManyAspectsB 2",
                "OnExit ManyAspectsA ManyAspectsA 2");
        }

        [Fact]
        public void ExceptionHandledByLastAspectShouldReturnItsValue()
        {
            new ManyAspectsMethods().Throw(1).Should().Be(42);

            ManyAspectsRecordingAspect.Calls.Should().Equal(
                "OnEntry ManyAspectsA",
                "OnEntry ManyAspectsB",
                "OnEntry ManyAspectsC",
                "OnEntry ManyAspectsReturningD",
                "OnException ManyAspectsReturningD ManyAspectsReturningD",
                "OnExit ManyAspectsC ManyAspectsC 42",
                "OnExit ManyAspectsB ManyAspectsB 42",
                "OnExit ManyAspectsA ManyAspectsA 42");
        }
    }
}
