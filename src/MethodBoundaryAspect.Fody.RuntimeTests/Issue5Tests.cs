using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// https://github.com/vescon/MethodBoundaryAspect.Fody/issues/5: an exception thrown by OnException of an
    /// async method (e.g. to wrap the original exception) escaped the state machine instead of faulting the
    /// returned task. It has to fault the task, like it is rethrown to the caller of a synchronous method.
    /// </summary>
    [Collection(nameof(Issue5Tests))]
    public class Issue5Tests
    {
        private readonly Issue5Methods _target = new Issue5Methods();

        public Issue5Tests()
        {
            Issue5WrappingAspect.Calls.Clear();
        }

        [Fact]
        public async Task ExceptionThrownByOnExceptionShouldFaultTask()
        {
            Func<Task> action = () => _target.ThrowAfterAwait();

            await ShouldThrowWrappedException(action);
            Issue5WrappingAspect.Calls.Should().Equal("OnException Issue5WrappingAspect An exception");
        }

        [Fact]
        public async Task ExceptionThrownByOnExceptionShouldFaultTaskWithResult()
        {
            Func<Task> action = () => _target.ThrowAfterAwaitWithResult();

            await ShouldThrowWrappedException(action);
        }

        [Fact]
        public async Task ExceptionThrownByOnExceptionShouldFaultValueTask()
        {
            Func<Task> action = async () => await _target.ThrowInValueTask();

            await ShouldThrowWrappedException(action);
        }

        [Fact]
        public async Task ExceptionThrownByOnExceptionShouldFaultTaskOfStaticMethod()
        {
            Func<Task> action = () => Issue5Methods.ThrowInStaticMethod();

            await ShouldThrowWrappedException(action);
        }

        [Fact]
        public async Task ExceptionThrownByOnExceptionShouldFaultTaskOfMethodWithTryCatch()
        {
            Func<Task> action = () => _target.ThrowInTryCatch();

            await ShouldThrowWrappedException(action);
        }

        [Fact]
        public async Task ExceptionThrownByOnExceptionBeforeFirstAwaitShouldFaultTaskInsteadOfThrowingSynchronously()
        {
            Task task = null;
            Action call = () => task = _target.ThrowBeforeAwait();

            call.Should().NotThrow();
            task.IsFaulted.Should().BeTrue();
            await ShouldThrowWrappedException(() => task);
        }

        [Fact]
        public async Task ExceptionThrownByLastOnExceptionShouldFaultTaskAfterOtherAspectsWereCalled()
        {
            Func<Task> action = () => _target.ThrowWithWrappingAspectCalledLast();

            await ShouldThrowWrappedException(action);
            Issue5WrappingAspect.Calls.Should().Equal(
                "OnException Issue5RecordingAspect An exception",
                "OnException Issue5WrappingAspect An exception");
        }

        [Fact]
        public async Task ExceptionThrownByFirstOnExceptionShouldSkipOtherAspects()
        {
            Func<Task> action = () => _target.ThrowWithWrappingAspectCalledFirst();

            await ShouldThrowWrappedException(action);
            Issue5WrappingAspect.Calls.Should().Equal("OnException Issue5WrappingAspect An exception");
        }

        private static async Task ShouldThrowWrappedException(Func<Task> action)
        {
            (await action.Should().ThrowAsync<WrappedException>().WithMessage("Wrapped"))
                .WithInnerException<InvalidOperationException>().WithMessage("An exception");
        }
    }
}
