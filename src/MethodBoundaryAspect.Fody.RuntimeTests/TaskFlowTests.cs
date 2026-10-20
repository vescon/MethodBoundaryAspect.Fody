using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// A method returning a Task which an aspect leaves (FlowBehavior.Return, swallowed exception) without a
    /// ReturnValue returned null, awaiting it threw a NullReferenceException. It returns a completed task now.
    /// </summary>
    [Collection(nameof(TaskFlowMethods))]
    public class TaskFlowTests
    {
        private readonly TaskFlowMethods _target = new TaskFlowMethods();

        public TaskFlowTests()
        {
            TaskFlowMethods.BodyWasCalled = false;
        }

        [Fact]
        public async Task AsyncTaskShouldReturnACompletedTask()
        {
            var task = _target.AsyncTask();

            task.Should().NotBeNull();
            await task;
            task.IsCompleted.Should().BeTrue();
            TaskFlowMethods.BodyWasCalled.Should().BeFalse();
        }

        [Fact]
        public async Task AsyncTaskOfIntShouldReturnATaskWithTheDefaultValue()
        {
            (await _target.AsyncTaskOfInt()).Should().Be(0);
            TaskFlowMethods.BodyWasCalled.Should().BeFalse();
        }

        [Fact]
        public async Task AsyncGenericShouldReturnATaskWithTheDefaultValue()
        {
            (await _target.AsyncGeneric(5)).Should().Be(0);
            (await _target.AsyncGeneric("text")).Should().BeNull();
            TaskFlowMethods.BodyWasCalled.Should().BeFalse();
        }

        [Fact]
        public async Task TaskReturningMethodShouldReturnATaskWithTheDefaultValue()
        {
            (await _target.SyncTaskOfString()).Should().BeNull();
            await _target.SyncTask();
            TaskFlowMethods.BodyWasCalled.Should().BeFalse();
        }

        [Fact]
        public async Task SwallowedExceptionShouldReturnACompletedTask()
        {
            await _target.SyncTaskThrowing();
            (await _target.SyncTaskOfIntThrowing()).Should().Be(0);
        }

        [Fact]
        public async Task TaskSetByTheAspectShouldBeReturned()
        {
            (await _target.AsyncWithReturnValue()).Should().Be(7);
            TaskFlowMethods.BodyWasCalled.Should().BeFalse();
        }
    }
}
