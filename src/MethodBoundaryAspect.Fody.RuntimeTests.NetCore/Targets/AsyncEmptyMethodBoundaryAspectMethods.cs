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

        [EmptyMethodBoundaryAspect]
        public async Task TryCatchEmptyMethodBoundaryAspectMethod()
        {
            try
            {
                await Task.CompletedTask;
            }
            catch
            {
            }
        }

        [EmptyMethodBoundaryAspect]
        public async Task TryFinallyEmptyMethodBoundaryAspectMethod()
        {
            try
            {
                await Task.CompletedTask;
            }
            finally
            {
                await Task.CompletedTask;
            }
        }

        [EmptyMethodBoundaryAspect]
        public async Task TryCatchFinallyEmptyMethodBoundaryAspectMethod()
        {
            try
            {
                await Task.CompletedTask;
            }
            catch
            {
            }
            finally
            {
                await Task.CompletedTask;
            }
        }
    }
}
