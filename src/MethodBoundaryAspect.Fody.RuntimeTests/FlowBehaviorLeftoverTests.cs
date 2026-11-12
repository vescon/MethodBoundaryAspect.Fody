using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// FlowBehavior.Continue set in OnEntry has no effect and must not survive until OnException: the exception
    /// of the method is only swallowed if an OnException asks for it.
    /// </summary>
    [Collection(nameof(ContinueInEntryAttribute))]
    public class FlowBehaviorLeftoverTests
    {
        public FlowBehaviorLeftoverTests()
        {
            lock (ContinueInEntryAttribute.SeenInOnException)
                ContinueInEntryAttribute.SeenInOnException.Clear();
        }

        [Fact]
        public void ContinueSetInOnEntryShouldNotSwallowTheException()
        {
            Action action = () => new FlowBehaviorLeftoverMethods().Throwing();

            action.Should().Throw<InvalidOperationException>().WithMessage("failed");
            ContinueInEntryAttribute.SeenInOnException.Should().Equal(FlowBehavior.Default);
        }

        [Fact]
        public async Task ContinueSetInOnEntryShouldNotSwallowTheExceptionOfAnAsyncMethod()
        {
            Func<Task> action = () => new FlowBehaviorLeftoverMethods().AsyncThrowing();

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("failed");
            ContinueInEntryAttribute.SeenInOnException.Should().Equal(FlowBehavior.Default);
        }

        [Fact]
        public async Task ContinueSetInOnEntryShouldNotSwallowTheExceptionOfAnAsyncTaskMethod()
        {
            Func<Task> action = () => new FlowBehaviorLeftoverMethods().AsyncTaskThrowing();

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("failed");
            ContinueInEntryAttribute.SeenInOnException.Should().Equal(FlowBehavior.Default);
        }
    }
}
