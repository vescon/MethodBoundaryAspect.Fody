using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// Behavior described in the README.
    /// </summary>
    [Collection(nameof(NameRecordingAspect))]
    public class RepeatedAspectTests
    {
        public RepeatedAspectTests()
        {
            NameRecordingAspect.Calls.Clear();
            ExceptionInEntryAndExit.Calls.Clear();
        }

        [Fact]
        public void AnAspectAppliedToTheClassAndTheMethodIsCalledOncePerApplication()
        {
            var target = new AspectOnClassAndMethod();

            target.Both();
            target.ClassOnly();

            NameRecordingAspect.Calls.Should().Equal("Both", "Both", "ClassOnly");
        }

        [Fact]
        public void AnExceptionInOnExitDoesNotCallOnException()
        {
            Action call = () => new ExceptionInEntryAndExit().Method();

            call.Should().Throw<InvalidOperationException>().WithMessage("from OnExit");
            ExceptionInEntryAndExit.Calls.Should().Equal("OnEntry", "body", "OnExit");
        }
    }
}
