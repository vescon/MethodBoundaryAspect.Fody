using System.Collections.Generic;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    // the aspect overrides no aspect method, the async method is not changed
    [NoAspectMethodsAspect]
    public class AsyncWithoutAspectMethods
    {
        public async Task<int> Async()
        {
            await Task.Yield();
            return 1;
        }
    }

    [OnlyOnEntryAspect]
    public class StateMachineMethods
    {
        public async Task<int> AsyncMethod(int value)
        {
            await Task.Yield();
            return value;
        }

        public IEnumerable<int> IteratorMethod(int count)
        {
            for (var i = 0; i < count; i++)
                yield return i;
        }
    }
}
