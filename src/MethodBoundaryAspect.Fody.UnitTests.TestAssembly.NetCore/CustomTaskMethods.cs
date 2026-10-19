using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    /// <summary>
    /// Async methods returning a task-like type with its own async method builder (like UniTask),
    /// not one of the builders of the compiler.
    /// </summary>
    public class CustomTaskMethods
    {
        [OnlyOnEntryAspect]
        public async CustomTask WithOnEntry()
        {
            await Task.Yield();
        }

        [OnlyOnExceptionAspect]
        public async CustomTask WithOnException()
        {
            await Task.Yield();
        }
    }

    [AsyncMethodBuilder(typeof(CustomTaskMethodBuilder))]
    public class CustomTask
    {
        public CustomTask(Task task) => Task = task;

        public Task Task { get; }

        public TaskAwaiter GetAwaiter() => Task.GetAwaiter();
    }

    public struct CustomTaskMethodBuilder
    {
        private AsyncTaskMethodBuilder _builder;

        public static CustomTaskMethodBuilder Create() => new CustomTaskMethodBuilder { _builder = AsyncTaskMethodBuilder.Create() };

        public CustomTask Task => new CustomTask(_builder.Task);

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            _builder.Start(ref stateMachine);

        public void SetStateMachine(IAsyncStateMachine stateMachine) => _builder.SetStateMachine(stateMachine);

        public void SetResult() => _builder.SetResult();

        public void SetException(Exception exception) => _builder.SetException(exception);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            _builder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            _builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }
}
