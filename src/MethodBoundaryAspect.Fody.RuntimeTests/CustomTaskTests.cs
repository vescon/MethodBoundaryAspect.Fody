using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// OnException is woven into the catch block of MoveNext which calls SetException of the async method builder
    /// of the compiler. A method with another builder (e.g. UniTask) is not weaved then, the build must not fail.
    /// </summary>
    [Collection(nameof(CustomTaskRecordingAspect))]
    public class CustomTaskTests
    {
        public CustomTaskTests()
        {
            CustomTaskRecordingAspect.Calls.Clear();
        }

        [Fact]
        public async Task AspectWithoutOnExceptionShouldBeWeavedIntoMethodWithCustomBuilder()
        {
            var result = await new CustomTaskMethods().WithEntryAndExit(1);

            result.Should().Be(2);
            CustomTaskRecordingAspect.Calls.Should().Equal("OnEntry WithEntryAndExit", "OnExit WithEntryAndExit");
        }

        [Fact]
        public async Task MethodWithOnExceptionAndCustomBuilderShouldRunWithoutAspects()
        {
            var result = await new CustomTaskMethods().WithOnException(1);

            result.Should().Be(2);
            CustomTaskRecordingAspect.Calls.Should().BeEmpty();
        }
    }
}
