using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class ValueTaskMethods
    {
        [ValueTaskRecordingAspect]
        public async ValueTask Throw()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }

        [ValueTaskRecordingAspect]
        public async ValueTask<int> ThrowWithResult()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }

        [ValueTaskRecordingAspect]
        public async ValueTask<int> Return(int value)
        {
            await Task.Delay(10);
            return value;
        }

        [ValueTaskSwallowingAspect]
        public async ValueTask ThrowAndSwallow()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }

        [ValueTaskSwallowingAspect]
        public async ValueTask<int> ThrowAndReplaceResult()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }
    }
}
