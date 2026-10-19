using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    /// <summary>
    /// Async methods returning a task-like type with its own async method builder (like UniTask),
    /// not one of the builders of the compiler.
    /// </summary>
    public class CustomTaskMethods
    {
        [CustomTaskRecordingAspect]
        public async CustomTask<int> WithEntryAndExit(int value)
        {
            await Task.Yield();
            return value + 1;
        }

        // OnException can't be woven into the state machine of another async method builder
        [EmptyMethodBoundaryAspect]
        [CustomTaskRecordingAspect]
        public async CustomTask<int> WithOnException(int value)
        {
            await Task.Yield();
            return value + 1;
        }
    }

    [AsyncMethodBuilder(typeof(CustomTaskMethodBuilder<>))]
    public class CustomTask<T>
    {
        public CustomTask(Task<T> task) => Task = task;

        public Task<T> Task { get; }

        public TaskAwaiter<T> GetAwaiter() => Task.GetAwaiter();
    }

    public struct CustomTaskMethodBuilder<T>
    {
        private AsyncTaskMethodBuilder<T> _builder;

        public static CustomTaskMethodBuilder<T> Create() => new CustomTaskMethodBuilder<T> { _builder = AsyncTaskMethodBuilder<T>.Create() };

        public CustomTask<T> Task => new CustomTask<T>(_builder.Task);

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            _builder.Start(ref stateMachine);

        public void SetStateMachine(IAsyncStateMachine stateMachine) => _builder.SetStateMachine(stateMachine);

        public void SetResult(T result) => _builder.SetResult(result);

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
