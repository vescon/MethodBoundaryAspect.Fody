using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// The tag set in OnExit of an async method is seen in OnException, which runs later when the work fails: the
    /// README shows it as the way to find out if the method already returned its task. It worked with one aspect only.
    /// </summary>
    [Collection(nameof(ExitFlagAttribute))]
    public class AsyncTagTests
    {
        public AsyncTagTests()
        {
            lock (ExitFlagAttribute.Seen)
                ExitFlagAttribute.Seen.Clear();
        }

        [Fact]
        public async Task TagSetInOnExitShouldBeSeenInOnExceptionWithOneAspect()
        {
            var gate = new TaskCompletionSource<bool>();
            var task = new AsyncTagMethods().OneAspect(gate.Task);

            gate.SetResult(true); // the method returned its task, OnExit was called
            Func<Task> action = () => task;
            await action.Should().ThrowAsync<InvalidOperationException>();

            ExitFlagAttribute.Seen.Should().Equal("OnException tag=True");
        }

        [Fact]
        public async Task TagSetInOnExitShouldBeSeenInOnExceptionWithSeveralAspects()
        {
            var gate = new TaskCompletionSource<bool>();
            var task = new AsyncTagMethods().TwoAspects(gate.Task);

            gate.SetResult(true);
            Func<Task> action = () => task;
            await action.Should().ThrowAsync<InvalidOperationException>();

            ExitFlagAttribute.Seen.Should().Equal("OnException tag=True");
        }
    }
}
