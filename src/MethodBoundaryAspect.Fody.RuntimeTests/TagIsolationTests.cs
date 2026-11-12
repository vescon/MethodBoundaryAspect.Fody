using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// Each aspect has its own MethodExecutionTag: an aspect which does not set the tag sees null, not the tag
    /// of the aspect before it.
    /// </summary>
    public class TagIsolationTests
    {
        public TagIsolationTests()
        {
            lock (TagObserverAttribute.Seen)
                TagObserverAttribute.Seen.Clear();
        }

        [Fact]
        public void TagOfAnotherAspectIsNotVisibleInSyncMethod()
        {
            new TagIsolationMethods().Sync();

            TagObserverAttribute.Seen.Should().Equal("OnEntry null", "OnExit null");
        }

        [Fact]
        public void TagOfAnotherAspectIsNotVisibleInOnException()
        {
            Action action = () => new TagIsolationMethods().Throwing();
            action.Should().Throw<InvalidOperationException>();

            TagObserverAttribute.Seen.Should().Equal("OnEntry null", "OnException null");
        }

        [Fact]
        public async Task TagOfAnotherAspectIsNotVisibleInAsyncMethod()
        {
            await new TagIsolationMethods().Async();

            TagObserverAttribute.Seen.Should().Equal("OnEntry null", "OnExit null");
        }

        [Fact]
        public async Task TagOfAnotherAspectIsNotVisibleInAsyncOnException()
        {
            Func<Task> action = () => new TagIsolationMethods().AsyncThrowing();
            await action.Should().ThrowAsync<InvalidOperationException>();

            // OnExit is called when the task is returned, OnException when the work fails, on another thread: the
            // order of the two is not defined
            TagObserverAttribute.Seen.Should().BeEquivalentTo("OnEntry null", "OnExit null", "OnException null");
        }
    }
}
