using System;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class AsyncReturnValueRaceMethods
    {
        [SlowOnExit, SwallowWithResult]
        public async Task<int> TaskOfInt()
        {
            await Task.Yield();
            throw new InvalidOperationException("failed");
        }

        [SlowOnExit, SwallowWithResult]
        public async ValueTask<int> ValueTaskOfInt()
        {
            await Task.Yield();
            throw new InvalidOperationException("failed");
        }
    }
}
