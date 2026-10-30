using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// Cases of the runtime behavior which were only tested while weaving, or not at all. The tests use their own
    /// aspects and targets, so they don't depend on the other tests.
    /// </summary>
    [Collection("Coverage")]
    public class CoverageTests
    {
        public CoverageTests()
        {
            CoverageLog.Reset();
        }

        [Fact]
        public async Task IfAnAsyncVoidMethodFailsAndTheAspectSwallowsTheException_ThenTheProcessSurvives()
        {
            new CoverageMethods().AsyncVoidThrow();

            // without the swallowing the exception is thrown on the thread pool and terminates the test run
            var message = await Task.WhenAny(CoverageLog.SwallowedException.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            message.Should().BeSameAs(CoverageLog.SwallowedException.Task);
            (await CoverageLog.SwallowedException.Task).Should().Be("async void failed");
        }

        [Fact]
        public async Task IfAnAspectSkipsAValueTaskMethod_ThenTheReturnValueOfTheAspectIsReturned()
        {
            (await new CoverageMethods().SkippedWithResult()).Should().Be(5);
        }

        [Fact]
        public async Task IfAnAspectSkipsAValueTaskMethodWithoutReturnValue_ThenTheDefaultIsReturned()
        {
            (await new CoverageMethods().SkippedWithoutResult()).Should().Be(0);
            await new CoverageMethods().Skipped();
        }

        [Fact]
        public async Task IfOnEntrySetsContinue_ThenTheValueTaskMethodRuns()
        {
            (await new CoverageMethods().ContinueInOnEntry()).Should().Be(7);
        }

        [Fact]
        public async Task IfOnEntrySetsRethrowException_ThenTheValueTaskMethodRuns()
        {
            (await new CoverageMethods().RethrowInOnEntry()).Should().Be(8);
        }

        [Fact]
        public void IfAMethodOfAStructIsWeaved_ThenItWorksOnTheStructItself()
        {
            var value = new CoverageStruct { Value = 1 };

            value.Increment();
            var sum = value.Add(10);

            value.Value.Should().Be(2);
            sum.Should().Be(12);
            CoverageLog.Calls.Should().Equal("OnEntry Increment", "OnExit Increment", "OnEntry Add", "OnExit Add");
        }

#if NETCOREAPP
        [Fact]
        public unsafe void IfAMethodHasAFunctionPointer_ThenItIsPassedUnchanged()
        {
            new CoverageMethods().CallFunctionPointer(&CoverageMethods.Twice, 21).Should().Be(42);
        }
#endif

        [Fact]
        public async Task IfSeveralThreadsCallAWeavedMethodForTheFirstTime_ThenAllGetTheSameMethodInfo()
        {
            const int threads = 16;
            using (var barrier = new Barrier(threads))
            {
                var tasks = Enumerable.Range(0, threads).Select(i => Task.Factory.StartNew(() =>
                {
                    barrier.SignalAndWait();
                    return CoverageMethods.FirstCallOfASharedAspect(i);
                }, TaskCreationOptions.LongRunning)).ToArray();

                (await Task.WhenAll(tasks)).Should().BeEquivalentTo(Enumerable.Range(0, threads));
            }

            CoverageLog.Methods.Should().HaveCount(threads);
            CoverageLog.Methods.Select(m => m.Name).Distinct().Should().Equal(nameof(CoverageMethods.FirstCallOfASharedAspect));
            CoverageLog.Methods.Distinct().Should().HaveCount(1, "the method info is created once");
        }
    }
}
