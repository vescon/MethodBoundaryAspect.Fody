[Back to the README](../README.md)

# Async methods

Aspects work on `async` methods returning `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` and `void`, but the timing differs from synchronous methods (see [When the aspect methods are called](../README.md#when-the-aspect-methods-are-called)). Applied to

```csharp
[Log]
public async Task MethodAsync()
{
    Console.WriteLine("Entering original method");
    await OtherClass.DoSomeOtherWorkAsync();
    Console.WriteLine("Exiting original method");
}
```

- `OnEntry` runs when `MethodAsync` is called, followed by "Entering original method" on the same thread.
- `OnExit` runs when `MethodAsync` returns its `Task` to the caller, synchronously on the calling thread. This may be long before the awaited work has completed.
- "Exiting original method" is written after the task of `DoSomeOtherWorkAsync` has completed, possibly on another thread.
- `OnException` runs if the awaited work fails (a faulted task, an exception, or awaiting `null`), on the thread on which the exception occurred, possibly long after `OnExit`.

So, unlike for synchronous methods, `OnExit` is called whether or not `OnException` is called. `OnException` can even run **before** `OnExit`: if the method throws before its first `await` that does not complete synchronously, the exception is handled while the method is still running. An exception thrown by `OnEntry` or `OnExit` itself is thrown synchronously to the caller, it is not stored in the returned task.

If `OnException` should only handle exceptions thrown after the method returned, track it with the `MethodExecutionTag`:

```csharp
public sealed class LogAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        Console.WriteLine("On entry");
        args.MethodExecutionTag = false;
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        Console.WriteLine("On exit");
        args.MethodExecutionTag = true;
    }

    public override void OnException(MethodExecutionArgs args)
    {
        if ((bool)args.MethodExecutionTag)
            return;

        Console.WriteLine("On exception");
    }
}
```

With several aspects each aspect has its own tag, the value set in `OnExit` is seen in `OnException`. Known issue: `OnExit` runs on the calling thread while the method returns its task, `OnException` runs in the state machine, possibly on another thread. If the work fails at that moment both run at the same time on the same `MethodExecutionArgs`, so the tag, `ReturnValue` and `Exception` are not safe to use in both. This only happens for a failure right around the first `await`.

The aspect is called once per call of the async method, also if it is applied to the class or assembly: the compiler-generated state machine (its `MoveNext` method runs each time the method resumes after an `await`) is not woven, so `args.Method` is always the async method itself ([#66](https://github.com/vescon/MethodBoundaryAspect.Fody/issues/66)).

To run code when the asynchronous work has finished, continue the returned task in `OnExit`:

```csharp
public override void OnExit(MethodExecutionArgs args)
{
    if (args.ReturnValue is Task task)
        task.ContinueWith(t => Console.WriteLine("Asynchronous work finished"));
}
```

Replacing the returned task changes the result the caller awaits, e.g. to turn a failure into a fallback value:

```csharp
public sealed class HandleExceptionAttribute : OnMethodBoundaryAspect
{
    public override void OnExit(MethodExecutionArgs args)
    {
        if (args.ReturnValue is Task<string> task)
        {
            args.ReturnValue = task.ContinueWith(t =>
                t.IsFaulted ? "An error happened: " + t.Exception.InnerException.Message : t.Result);
        }
    }
}

[HandleException]
public static async Task<string> Process()
{
    await Task.Delay(10);
    throw new Exception("Bad data");
}

// await Process() returns "An error happened: Bad data"
```

## Task-like types with their own async method builder

`OnException` is woven into the catch block of the compiler-generated state machine, which completes the async method builder of the compiler (`AsyncTaskMethodBuilder`, `AsyncValueTaskMethodBuilder`, `AsyncVoidMethodBuilder`, ...). Methods returning a task-like type with its own builder (e.g. `UniTask`) can only be woven if their aspects use `OnEntry` and `OnExit` only. A task-like type implemented as a class has no default value other than `null`: if an aspect skips the method body or swallows an exception without setting `args.ReturnValue`, the method returns `null`. A method which also has an aspect with `OnException` is not changed at all, and a build warning is written ("... is not woven"). Exclude the method from weaving with `[DisableWeaving]` to remove the warning.

## FlowBehavior in async methods

- `FlowBehavior.Return` in `OnEntry`: `args.ReturnValue` is what the method returns, so it has to be a task, e.g. `args.ReturnValue = Task.FromResult(42);`. If no aspect sets it, the method returns a completed task (with the default value as result for `Task<T>`).
- `FlowBehavior.Continue` in `OnException`: the returned task completes successfully with `args.ReturnValue` as its result (the value, not a task), e.g. `args.ReturnValue = 0;` for a `Task<int>`. If no aspect sets it, the result is the default value. For `Task<T>` and `ValueTask<T>`, `args.ReturnValue` is set to `null` before `OnException` is called, the task returned to the caller (provided to `OnExit`) is never the result. For `Task`, `ValueTask` and `void` methods `args.ReturnValue` is not changed and still holds the returned task (or `null` if the method had not returned it yet).
- An exception thrown in `OnException` (e.g. to wrap the original exception) faults the returned task with that exception, like it is thrown to the caller of a synchronous method. The `OnException` of the remaining aspects is not called.
