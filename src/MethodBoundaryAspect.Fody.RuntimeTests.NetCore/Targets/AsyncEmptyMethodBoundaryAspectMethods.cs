using MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Aspects;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Targets
{
    public class AsyncEmptyMethodBoundaryAspectMethods
    {
        [EmptyMethodBoundaryAspect]
        public async Task IfEmptyMethodBoundaryAspectMethod(bool test)
        {
            if (test)
            {
                await Task.CompletedTask;
            }
        }

        [EmptyMethodBoundaryAspect]
        public async Task IfElseEmptyMethodBoundaryAspectMethod(bool test)
        {
            if (test)
            {
                await Task.CompletedTask;
            }
            else
            {
                await Task.CompletedTask;
            }
        }
    }
}
