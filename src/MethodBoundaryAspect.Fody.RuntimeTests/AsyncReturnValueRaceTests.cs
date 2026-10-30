using System.Threading.Tasks;
using FluentAssertions;
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
    public class AsyncReturnValueRaceTests
    {
        [Fact]
        public async Task TaskShouldBeReturnedIfTheWorkFailsDuringOnExit()
        {
            var task = new AsyncReturnValueRaceMethods().TaskOfInt();

            (await task).Should().Be(-1);
        }

        [Fact]
        public async Task ValueTaskShouldBeReturnedIfTheWorkFailsDuringOnExit()
        {
            var task = new AsyncReturnValueRaceMethods().ValueTaskOfInt();

            (await task).Should().Be(-1);
        }
    }
}
