using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// OnException only guards the method body: an exception thrown by OnEntry or OnExit does not call it and is
    /// thrown to the caller, also for async methods where it is thrown synchronously and not stored in the task.
    /// </summary>
    [Collection(nameof(ThrowingBoundaryAttribute))]
    public class ThrowingBoundaryTests
    {
        private readonly ThrowingBoundaryMethods _target = new ThrowingBoundaryMethods();

        public ThrowingBoundaryTests()
        {
            ThrowingBoundaryAttribute.Calls.Clear();
        }

        [Fact]
        public void IfOnEntryThrowsInAVoidMethod_ThenOnlyOnEntryRunsAndTheExceptionIsThrown()
        {
            Action call = () => _target.VoidEntry();

            call.Should().Throw<InvalidOperationException>().WithMessage("entry");
            ShouldHaveRun("OnEntry");
        }

        [Fact]
        public void IfOnEntryThrowsInAMethodWithResult_ThenOnlyOnEntryRunsAndTheExceptionIsThrown()
        {
            Action call = () => _target.IntEntry();

            call.Should().Throw<InvalidOperationException>().WithMessage("entry");
            ShouldHaveRun("OnEntry");
        }

        [Fact]
        public void IfOnEntryThrowsInATaskMethod_ThenTheExceptionIsThrownSynchronously()
        {
            Action call = () => { _ = _target.TaskEntry(); };

            call.Should().Throw<InvalidOperationException>().WithMessage("entry");
            ShouldHaveRun("OnEntry");
        }

        [Fact]
        public void IfOnEntryThrowsInAValueTaskMethod_ThenTheExceptionIsThrownSynchronously()
        {
            Action call = () => { _ = _target.ValueTaskEntry(); };

            call.Should().Throw<InvalidOperationException>().WithMessage("entry");
            ShouldHaveRun("OnEntry");
        }

        [Fact]
        public void IfOnExitThrowsInAVoidMethod_ThenOnExceptionIsNotCalledAndTheExceptionIsThrown()
        {
            Action call = () => _target.VoidExit();

            call.Should().Throw<InvalidOperationException>().WithMessage("exit");
            ShouldHaveRun("OnEntry", "OnExit");
            _target.BodyRuns.Should().Be(1);
        }

        [Fact]
        public void IfOnExitThrowsInAMethodWithResult_ThenOnExceptionIsNotCalledAndTheExceptionIsThrown()
        {
            Action call = () => _target.IntExit();

            call.Should().Throw<InvalidOperationException>().WithMessage("exit");
            ShouldHaveRun("OnEntry", "OnExit");
            _target.BodyRuns.Should().Be(1);
        }

        [Fact]
        public void IfOnExitThrowsInATaskMethod_ThenTheExceptionIsThrownSynchronously()
        {
            Action call = () => { _ = _target.TaskExit(); };

            call.Should().Throw<InvalidOperationException>().WithMessage("exit");
            ShouldHaveRun("OnEntry", "OnExit");
        }

        [Fact]
        public void IfOnExitThrowsInAValueTaskMethod_ThenTheExceptionIsThrownSynchronously()
        {
            Action call = () => { _ = _target.ValueTaskExit(); };

            call.Should().Throw<InvalidOperationException>().WithMessage("exit");
            ShouldHaveRun("OnEntry", "OnExit");
        }

        private void ShouldHaveRun(params string[] calls)
        {
            lock (ThrowingBoundaryAttribute.Calls)
                ThrowingBoundaryAttribute.Calls.Should().Equal(calls);

            if (calls.Length == 1)
                _target.BodyRuns.Should().Be(0, "the body is not run if OnEntry throws");
        }
    }
}
