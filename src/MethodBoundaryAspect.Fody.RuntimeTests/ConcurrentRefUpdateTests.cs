using System.Threading.Tasks;
using AwesomeAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// With [AllowChangingInputArguments] the method gets the variable of the caller, but the value from the arguments
    /// array was written into it before the call, also if no aspect changed it: an update of another thread
    /// while OnEntry ran was lost (Interlocked.Add(ref shared, 1) in a method with such an aspect).
    /// </summary>
    [Collection(nameof(ConcurrentRefUpdateSignal))]
    public class ConcurrentRefUpdateTests
    {
        public ConcurrentRefUpdateTests()
        {
            ConcurrentRefUpdateSignal.Reset();
        }

        [Fact]
        public async Task ValueChangedByAnotherThreadShouldBeKeptIfNoAspectChangedTheArgument()
        {
            var value = 1;

            await RunWhileInOnEntry(() => value = 42, () => new ConcurrentRefUpdateMethods().KeepValue(ref value));

            value.Should().Be(42);
        }

        [Fact]
        public async Task ValueSetByAnAspectShouldBeWrittenToTheVariable()
        {
            var value = 1;

            await RunWhileInOnEntry(() => value = 42, () => new ConcurrentRefUpdateMethods().Replaced(ref value));

            value.Should().Be(100);
        }

        private static async Task RunWhileInOnEntry(System.Action changeVariable, System.Action call)
        {
            var task = Task.Run(call);

            ConcurrentRefUpdateSignal.InOnEntry.Wait(System.TimeSpan.FromSeconds(10)).Should().BeTrue();
            changeVariable();
            ConcurrentRefUpdateSignal.Continue.Set();

            await task;
        }
    }
}
