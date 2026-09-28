using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// https://github.com/vescon/MethodBoundaryAspect.Fody/issues/66: async methods woven with an aspect
    /// overriding OnException threw "Common Language Runtime detected an invalid program" when called.
    /// PEVerify does not detect it, so the woven methods have to be executed.
    /// </summary>
    [Collection(nameof(Issue66Tests))]
    public class Issue66Tests
    {
        private readonly Issue66Methods _target = new Issue66Methods();
        private readonly Issue66TimedMethods _timedTarget = new Issue66TimedMethods();

        public Issue66Tests()
        {
            Issue66OnExceptionAspect.Calls.Clear();
            Issue66TimingAspect.Calls.Clear();
        }

        [Fact]
        public async Task EmptyAsyncMethodsShouldRun()
        {
            await Issue66Methods.DoNothing();
            await _target.Invoke(new object());
            await _target.InstanceDoNothing();

            Issue66OnExceptionAspect.Calls.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public async Task AwaitInsideIfShouldRun(int value)
        {
            await Issue66Methods.DemoBugAsync(value);
            await _target.IfWithYield(value > 0);
        }

        [Fact]
        public async Task TryFinallyAroundAwaitShouldRun()
        {
            var log = new List<string>();

            await _target.TryFinallyAroundAwait(log);

            log.Should().Equal("finally");
        }

        [Theory]
        [InlineData(-3, -1)]
        [InlineData(0, 0)]
        [InlineData(4, 8)]
        public async Task EarlyReturnsShouldReturnResult(int value, int expected)
        {
            (await _target.EarlyReturns(value)).Should().Be(expected);
        }

        [Fact]
        public async Task LoopsShouldReturnResult()
        {
            (await _target.Loop(5, CancellationToken.None)).Should().Be(10);
            (await _target.Loop(5, new CancellationToken(true))).Should().Be(0);
            (await _target.WhileWithContinue(6)).Should().Be(1 + 3 + 5);
        }

        [Theory]
        [InlineData(0, "zero")]
        [InlineData(1, "one")]
        [InlineData(2, "one")]
        [InlineData(3, "many")]
        public async Task SwitchShouldReturnResult(int value, string expected)
        {
            (await _target.Switch(value)).Should().Be(expected);
        }

        [Theory]
        [InlineData(false, "try")]
        [InlineData(true, "caught")]
        public async Task AwaitInCatchAndFinallyShouldReturnResult(bool shouldThrow, string expected)
        {
            var log = new List<string>();

            (await _target.AwaitInCatchAndFinally(shouldThrow, log)).Should().Be(expected);

            log.Should().Equal("finally");
            Issue66OnExceptionAspect.Calls.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(5, 20)]
        public async Task NestedTryShouldReturnResult(int value, int expected)
        {
            (await _target.NestedTry(value)).Should().Be(expected);
            Issue66OnExceptionAspect.Calls.Should().BeEmpty();
        }

        [Fact]
        public async Task UsingDeclarationShouldReturnResult()
        {
            (await _target.UsingDeclaration()).Should().Be(3);
        }

        [Fact]
        public async Task GenericMethodShouldReturnResult()
        {
            (await _target.Generic("value", true)).Should().Be("value");
            (await _target.Generic(42, false)).Should().Be(42);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1 + 55)]
        public async Task LongMethodShouldReturnResult(int value, int expected)
        {
            (await _target.ManyAwaits(value)).Should().Be(expected);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AsyncVoidMethodShouldRun(bool flag)
        {
            var done = new TaskCompletionSource<bool>();

            _target.AsyncVoidWithIf(flag, done);

            (await done.Task).Should().Be(flag);
        }

        [Fact]
        public async Task ExceptionAfterYieldShouldBePassedToAspectAndRethrown()
        {
            await _target.ThrowAfterYield(false);
            Func<Task> action = () => _target.ThrowAfterYield(true);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("after yield");
            Issue66OnExceptionAspect.Calls.Should().Equal("OnException ThrowAfterYield after yield");
        }

        [Fact]
        public async Task ExceptionInsideIfShouldBePassedToAspectAndRethrown()
        {
            (await _target.ThrowInsideIf(0)).Should().Be(0);
            Func<Task> action = () => _target.ThrowInsideIf(1);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("inside if");
            Issue66OnExceptionAspect.Calls.Should().Equal("OnException ThrowInsideIf inside if");
        }

        [Fact]
        public async Task MethodsWithMultipleAspectsShouldRun()
        {
            var log = new List<string>();

            (await _timedTarget.Loop(3, CancellationToken.None)).Should().Be(3);
            await _timedTarget.IfWithYield(true);
            await _timedTarget.IfWithYield(false);
            await _timedTarget.TryFinally(log);

            log.Should().Equal("finally");
            Issue66TimingAspect.Calls.Should().Equal(
                "OnEntry Loop", "OnExit Loop",
                "OnEntry IfWithYield", "OnExit IfWithYield",
                "OnEntry IfWithYield", "OnExit IfWithYield",
                "OnEntry TryFinally", "OnExit TryFinally");
            Issue66OnExceptionAspect.Calls.Should().BeEmpty();
        }

        [Fact]
        public void IteratorStateMachineShouldNotBeWoven()
        {
            _timedTarget.Iterate(3).Should().Equal(0, 1, 2);

            Issue66TimingAspect.Calls.Should().Equal("OnEntry Iterate", "OnExit Iterate");
        }

        [Fact]
        public async Task ExceptionWithMultipleAspectsShouldBePassedToAllAspectsAndRethrown()
        {
            Func<Task> action = () => _timedTarget.ThrowAfterYield();

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("after yield");
            // the continuation after Task.Yield() may call OnException before OnExit is called on this thread
            Issue66TimingAspect.Calls.Should().StartWith("OnEntry ThrowAfterYield");
            Issue66TimingAspect.Calls.Should().BeEquivalentTo(
                "OnEntry ThrowAfterYield",
                "OnExit ThrowAfterYield",
                "OnException ThrowAfterYield after yield");
            Issue66OnExceptionAspect.Calls.Should().Equal("OnException ThrowAfterYield after yield");
        }
    }
}
