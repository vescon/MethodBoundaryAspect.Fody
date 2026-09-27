using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// "async ValueTask" methods use AsyncValueTaskMethodBuilder(`1) instead of AsyncTaskMethodBuilder(`1).
    /// Like "async void" methods (https://github.com/vescon/MethodBoundaryAspect.Fody/issues/111),
    /// weaving them for OnException failed with "did not catch exceptions in the expected way".
    /// </summary>
    public class AsyncValueTaskTests
    {
        public AsyncValueTaskTests()
        {
            ValueTaskRecordingAspect.Calls.Clear();
        }

        [Fact]
        public async Task ExceptionInValueTaskMethodShouldBePassedToAspectAndRethrown()
        {
            var target = new ValueTaskMethods();

            Func<Task> action = async () => await target.Throw();

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("An exception");
            ValueTaskRecordingAspect.Calls.Should().Equal(
                "OnEntry Throw",
                "OnExit Throw",
                "OnException Throw An exception");
        }

        [Fact]
        public async Task ExceptionInGenericValueTaskMethodShouldBePassedToAspectAndRethrown()
        {
            var target = new ValueTaskMethods();

            Func<Task> action = async () => await target.ThrowWithResult();

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("An exception");
            ValueTaskRecordingAspect.Calls.Should().Equal(
                "OnEntry ThrowWithResult",
                "OnExit ThrowWithResult",
                "OnException ThrowWithResult An exception");
        }

        [Fact]
        public async Task GenericValueTaskMethodShouldReturnResult()
        {
            var target = new ValueTaskMethods();

            (await target.Return(7)).Should().Be(7);

            ValueTaskRecordingAspect.Calls.Should().Equal(
                "OnEntry Return",
                "OnExit Return");
        }

        [Fact]
        public async Task ExceptionInValueTaskMethodShouldBeSwallowedByFlowBehaviorContinue()
        {
            var target = new ValueTaskMethods();

            Func<Task> action = async () => await target.ThrowAndSwallow();

            await action.Should().NotThrowAsync();
            ValueTaskRecordingAspect.Calls.Should().Equal("OnException ThrowAndSwallow An exception");
        }

        [Fact]
        public async Task ExceptionInGenericValueTaskMethodShouldBeReplacedByReturnValueOfAspect()
        {
            var target = new ValueTaskMethods();

            (await target.ThrowAndReplaceResult()).Should().Be(42);

            ValueTaskRecordingAspect.Calls.Should().Equal("OnException ThrowAndReplaceResult An exception");
        }
    }
}
