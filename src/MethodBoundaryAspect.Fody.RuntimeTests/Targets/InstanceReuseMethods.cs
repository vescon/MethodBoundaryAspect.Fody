using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    [SharedClassAspect]
    public class InstanceReuseClassMethods
    {
        public int First(int value) => value;

        public int Second(int value) => value;
    }

    public class InstanceReuseMethods
    {
        [SharedAspect("method")]
        public int Sync(int value) => value;

        [SharedAspect("async", Level = 2)]
        public async Task<int> Async(int value)
        {
            await Task.Yield();
            return value;
        }

        [SharedAspect("throwing")]
        public void Throwing() => throw new InvalidOperationException("failed");

        [SharedAspect("throwing", Level = 1)]
        public async Task ThrowingAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("failed async");
        }

        // the same shared aspect type twice: every attribute has its own instance and tag
        [SharedAspect("first")]
        [SharedAspect("second")]
        public int Two(int value) => value;

        [SharedAspect("first")]
        [SharedAspect("second")]
        public async Task<int> TwoAsync(int value)
        {
            await Task.Yield();
            return value;
        }

        [PerCallState]
        public void PerCall()
        {
        }

        [PerCallState]
        public async Task PerCallAsync() => await Task.Yield();
    }
}
