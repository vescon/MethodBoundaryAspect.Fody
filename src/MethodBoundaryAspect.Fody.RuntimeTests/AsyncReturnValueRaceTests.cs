using System.Threading.Tasks;
using AwesomeAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// The woven code of an async method sets the returned task as ReturnValue, calls OnExit and returns the
    /// ReturnValue. The asynchronous work can fail on another thread in the meantime, where the catch block of
    /// MoveNext replaces the ReturnValue by the result of the swallowed exception: the caller got an
    /// InvalidCastException (or a default value) instead of its task.
    /// </summary>
    [Collection(nameof(AsyncReturnValueRaceSignal))]
    public class AsyncReturnValueRaceTests
    {
        public AsyncReturnValueRaceTests()
        {
            AsyncReturnValueRaceSignal.ExceptionHandled.Reset();
            AsyncReturnValueRaceSignal.WaitedForException = false;
        }

        [Fact]
        public async Task TaskShouldBeReturnedIfTheWorkFailsDuringOnExit()
        {
            // called on a thread pool thread: the continuation after Task.Yield() must not need the thread blocked in OnExit
            Task<int> task = null;
            await Task.Run(() => task = new AsyncReturnValueRaceMethods().TaskOfInt());

            (await task).Should().Be(-1);
            AsyncReturnValueRaceSignal.WaitedForException.Should().BeTrue("the work has to fail while OnExit runs");
        }

        [Fact]
        public async Task ValueTaskShouldBeReturnedIfTheWorkFailsDuringOnExit()
        {
            ValueTask<int> task = default;
            await Task.Run(() => task = new AsyncReturnValueRaceMethods().ValueTaskOfInt());

            (await task).Should().Be(-1);
            AsyncReturnValueRaceSignal.WaitedForException.Should().BeTrue("the work has to fail while OnExit runs");
        }
    }
}
