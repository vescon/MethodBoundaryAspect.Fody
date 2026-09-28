using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    // https://github.com/vescon/MethodBoundaryAspect.Fody/issues/46
    public class InheritedExceptionAspectMethods
    {
        [LogExceptionAndThrow]
        public int ThrowAndRethrow()
        {
            throw new InvalidOperationException("thrown");
        }

        [LogExceptionAndSwallow]
        public int ThrowAndSwallow()
        {
            throw new InvalidOperationException("thrown");
        }

        [LogExceptionAndSwallow]
        public int NoThrow()
        {
            return 42;
        }

        [LogExceptionAndSwallow]
        public async Task<int> ThrowAndSwallowAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("thrown");
        }

        [LogEntryAndException]
        public int ThrowWithInheritedOnException()
        {
            throw new InvalidOperationException("thrown");
        }
    }
}
