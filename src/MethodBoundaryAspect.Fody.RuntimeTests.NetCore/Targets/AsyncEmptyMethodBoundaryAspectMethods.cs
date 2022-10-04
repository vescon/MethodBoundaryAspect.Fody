using MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Aspects;
using System.Collections.Generic;
using System.IO;
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

        [EmptyMethodBoundaryAspect]
        public async Task<bool> TryCatchFinallyEmptyMethodBoundaryAspectMethodWResult()
        {
            try
            {
                await Task.CompletedTask;

                return true;
            }
            catch
            {
                throw;
            }
        }

        [EmptyMethodBoundaryAspect]
        public async Task ForeachEmptyMethodBoundaryAspectMethod(IEnumerable<object> collection)
        {
            foreach (var obj in collection)
            {
                await Task.CompletedTask;
            }
        }

        [EmptyMethodBoundaryAspect]
        public async Task SwitchExprMethodBoundaryAspectMethod(bool flag)
        {
            await Task.CompletedTask;

            _ = flag switch
            {
                true => false,
                false => true,
            };
        }

        [EmptyMethodBoundaryAspect]
        public async Task UsingMethodBoundaryAspectMethod()
        {
            using (var _ = new MemoryStream())
            {
                await Task.CompletedTask;
            }
        }
    }
}
