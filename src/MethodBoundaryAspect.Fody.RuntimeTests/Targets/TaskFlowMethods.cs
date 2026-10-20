using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class TaskFlowMethods
    {
        public static bool BodyWasCalled;

        [ReturnInEntryFlow]
        public async Task AsyncTask()
        {
            BodyWasCalled = true;
            await Task.Yield();
        }

        [ReturnInEntryFlow]
        public async Task<int> AsyncTaskOfInt()
        {
            BodyWasCalled = true;
            await Task.Yield();
            return 42;
        }

        [ReturnInEntryFlow]
        public async Task<T> AsyncGeneric<T>(T value)
        {
            BodyWasCalled = true;
            await Task.Yield();
            return value;
        }

        [ReturnInEntryFlow]
        public Task<string> SyncTaskOfString()
        {
            BodyWasCalled = true;
            return Task.FromResult("body");
        }

        [ReturnInEntryFlow]
        public Task SyncTask()
        {
            BodyWasCalled = true;
            return Task.CompletedTask;
        }

        [SwallowExceptionFlow]
        public Task SyncTaskThrowing() => throw new InvalidOperationException();

        [SwallowExceptionFlow]
        public Task<int> SyncTaskOfIntThrowing() => throw new InvalidOperationException();

        [ReturnTaskInEntry]
        public async Task<int> AsyncWithReturnValue()
        {
            BodyWasCalled = true;
            await Task.Yield();
            return 42;
        }
    }
}
