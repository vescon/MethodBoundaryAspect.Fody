using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class ThrowingBoundaryMethods
    {
        public int BodyRuns { get; private set; }

        [ThrowingBoundary(ThrowInEntry = true)]
        public void VoidEntry() => BodyRuns++;

        [ThrowingBoundary(ThrowInEntry = true)]
        public int IntEntry() => ++BodyRuns;

        [ThrowingBoundary(ThrowInEntry = true)]
        public async Task<int> TaskEntry()
        {
            await Task.Yield();
            return ++BodyRuns;
        }

        [ThrowingBoundary(ThrowInEntry = true)]
        public async ValueTask<int> ValueTaskEntry()
        {
            await Task.Yield();
            return ++BodyRuns;
        }

        [ThrowingBoundary(ThrowInExit = true)]
        public void VoidExit() => BodyRuns++;

        [ThrowingBoundary(ThrowInExit = true)]
        public int IntExit() => ++BodyRuns;

        [ThrowingBoundary(ThrowInExit = true)]
        public async Task<int> TaskExit()
        {
            await Task.Yield();
            return ++BodyRuns;
        }

        [ThrowingBoundary(ThrowInExit = true)]
        public async ValueTask<int> ValueTaskExit()
        {
            await Task.Yield();
            return ++BodyRuns;
        }
    }
}
