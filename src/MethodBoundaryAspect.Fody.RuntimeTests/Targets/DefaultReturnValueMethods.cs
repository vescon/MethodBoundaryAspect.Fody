using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class DefaultReturnValueMethods
    {
        [SometimesWritingReturnValueAspect]
        public int CachedInt() => throw new InvalidOperationException("Body should be skipped");

        [SometimesWritingReturnValueAspect, SwallowExceptionAspect]
        public int SwallowedInt() => throw new InvalidOperationException("An exception");

        [SometimesWritingReturnValueAspect, SwallowExceptionAspect]
        public T SwallowedGeneric<T>() => throw new InvalidOperationException("An exception");

        [SwallowExceptionAspect]
        public async Task<int> SwallowedAfterAwaitAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("An exception");
        }

        [SwallowExceptionAspect]
        public async Task<int> SwallowedBeforeAwaitAsync()
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("An exception");
        }

        [SwallowAndReadReturnValueAspect]
        public async Task<string> SwallowedAfterAwaitWithReturnValueReadAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("An exception");
        }

        [SwallowAndReadReturnValueAspect]
        public async Task<int> SwallowedIntAfterAwaitWithReturnValueReadAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("An exception");
        }

        [SwallowWithReturnValueAspect]
        public async Task<int> SwallowedWithReturnValueAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("An exception");
        }
    }
}
