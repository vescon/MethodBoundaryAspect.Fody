[Back to the README](../README.md)

# The woven method: details

The README shows [how a method is woven](../README.md#how-it-works) and how an aspect [changes its behavior](../README.md#changing-the-method-behavior). This page has the edge cases.

## The executor method

The original body runs in the private method `$_executor_<Name>`:

- It shows up in stack traces next to the original method (the JIT may inline it, then it is missing).
- `MethodBase.GetCurrentMethod()` called in the method body returns the executor, not the original method.
- Code in the body that looks at its caller (`Assembly.GetCallingAssembly()`) sees the woven method instead of the real caller.
- `[SkipLocalsInit]` and `MethodImplOptions.NoOptimization`/`AggressiveOptimization` of the method apply to the executor, the other `MethodImpl` flags (e.g. `NoInlining`) stay on the original method.

Async methods have no executor, see [Async methods](async-methods.md).

## Exceptions of the aspect itself

Only the method body is guarded by `OnException`. An exception thrown by `OnEntry` or `OnExit` itself does not call `OnException` and is thrown to the caller. This also holds for async methods, where it is thrown synchronously and not stored in the returned task.

## The return value of a `ref` return

For a method with a `ref` return, `args.ReturnValue` is the value of the returned variable. Setting it writes through the reference:

- it is ignored for `ref readonly`
- `null` for a value type leaves the variable unchanged

## Changing input arguments

For an aspect with `[AllowChangingInputArguments]`:

- Only the elements of the array count: assigning a new array to `args.Arguments` is ignored.
- `ref` and `out` values are written back to the caller.
- An element of a value type set to `null` passes the default value of the type.
- A changed `in` (or `ref readonly`) argument is passed to the method, but not written back, because the variable of the caller is read only for the method.
- An `out` parameter the method didn't assign before it threw (and an aspect swallowed the exception) keeps the value the variable of the caller had before the call, the method works on that variable.
- It works for async methods as well: the changed values are passed to the method when it starts.
- Aspects derived from an annotated aspect class inherit the attribute.

## Skipping the method or swallowing exceptions

- If no aspect sets `args.ReturnValue`, the method returns the default value of its return type: a completed task for a method returning `Task` or `Task<T>`, not `null`.
- A method with a `ref` return has no default value to return: skipping its body or swallowing its exception throws a `NotSupportedException` (with the original exception as inner exception).
- `FlowBehavior` is reset to `Default` before the first `OnException`, so a `Continue` set in `OnEntry` (where it has no effect) does not swallow a later exception. It has no effect in `OnExit`.
- `FlowBehavior.Return` in `OnEntry` skips the method body and the `OnEntry` of the following aspects, and also their `OnExit`. The `OnExit` of the aspects ordered before the aspect that set it is still called.
- If an aspect swallows an exception in `OnException`, the aspects ordered before it get a call of `OnExit` (not of `OnException`).

For async methods see [FlowBehavior in async methods](async-methods.md#flowbehavior-in-async-methods).
