using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    [IteratorRecordingAspect]
    public class IteratorMethods
    {
        public IEnumerable<int> ThrowWhileEnumerating()
        {
            yield return 1;
            throw new InvalidOperationException("enumerating");
        }

#if NETCOREAPP
        public async IAsyncEnumerable<int> ThrowWhileEnumeratingAsync()
        {
            await Task.Yield();
            yield return 1;
            throw new InvalidOperationException("enumerating");
        }
#endif
    }
}
