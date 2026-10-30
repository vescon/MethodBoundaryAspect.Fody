using System;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class FlowBehaviorLeftoverMethods
    {
        [ContinueInEntry]
        public int Throwing() => throw new InvalidOperationException("failed");

        [ContinueInEntry]
        public async Task<int> AsyncThrowing()
        {
            await Task.Yield();
            throw new InvalidOperationException("failed");
        }

        [ContinueInEntry]
        public async Task AsyncTaskThrowing()
        {
            await Task.Yield();
            throw new InvalidOperationException("failed");
        }
    }
}
