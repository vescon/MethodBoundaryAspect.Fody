[![.github/workflows/main.yaml](https://github.com/vescon/MethodBoundaryAspect.Fody/actions/workflows/main.yaml/badge.svg?branch=master)](https://github.com/vescon/MethodBoundaryAspect.Fody/actions/workflows/main.yaml)
[![NuGet](https://img.shields.io/nuget/v/MethodBoundaryAspect.Fody.svg)](https://www.nuget.org/packages/MethodBoundaryAspect.Fody/)

# MethodBoundaryAspect.Fody

Run your own code when a method starts, ends or throws, by putting an attribute on it:

```csharp
[Log]
public int Add(int a, int b) => a + b;
```

MethodBoundaryAspect.Fody is a [Fody](https://github.com/Fody/Fody) weaver: the calls to your aspect are woven into the IL of the method **at build time**. There is no runtime proxy, no dependency injection container and no interface or virtual method required. It works for static, private, generic and async methods of any class, on .NET Framework 4.6.2+ and .NET (Core) / .NET Standard 2.0+.

Typical aspects: logging, measuring execution time, transaction handling, exception wrapping, showing a wait cursor, argument validation, caching.

The overhead is small: the weaver only provides what your aspects use, loads the method info once, and creates aspects without state once instead of for every call, see [Performance](#performance).

> Upgrading from version 2? See [Upgrading to version 3](#upgrading-to-version-3).

## Contents

- [Quickstart](#quickstart)
- [How it works](#how-it-works)
- [Writing aspects](#writing-aspects)
- [Applying aspects](#applying-aspects)
- [Changing the method behavior](#changing-the-method-behavior)
- [Async methods](#async-methods)
- [Ref structs](#ref-structs-spant-readonlyspant-)
- [Performance](#performance)
- [Configuration reference](#configuration-reference)
- [Build warnings and errors](#build-warnings-and-errors)
- [Upgrading to version 3](#upgrading-to-version-3)
- [Contributing](#contributing)

## Quickstart

1. Add the NuGet package (it brings Fody along):

   ```
   dotnet add package MethodBoundaryAspect.Fody
   ```

2. Add a `FodyWeavers.xml` file to the project root:

   ```xml
   <Weavers>
     <MethodBoundaryAspect />
   </Weavers>
   ```

3. Write an aspect by deriving from `OnMethodBoundaryAspect`:

   ```csharp
   using System;
   using MethodBoundaryAspect.Fody.Attributes;

   public sealed class LogAttribute : OnMethodBoundaryAspect
   {
       public override void OnEntry(MethodExecutionArgs args) =>
           Console.WriteLine($"Entering {args.Method.Name}({string.Join(", ", args.Arguments)})");

       public override void OnExit(MethodExecutionArgs args) =>
           Console.WriteLine($"{args.Method.Name} returned {args.ReturnValue}");

       public override void OnException(MethodExecutionArgs args) =>
           Console.WriteLine($"{args.Method.Name} failed: {args.Exception.Message}");
   }
   ```

4. Apply it and build:

   ```csharp
   public class Calculator
   {
       [Log]
       public int Add(int a, int b) => a + b;
   }

   new Calculator().Add(1, 2);
   // Entering Add(1, 2)
   // Add returned 3
   ```

Complete projects: [Samples/HelloWorld_NetCore](Samples/HelloWorld_NetCore) and [Samples/HelloWorld_NetFramework](Samples/HelloWorld_NetFramework).

## How it works

When the project is built, Fody runs the weaver on the compiled assembly. For every method hit by an aspect, the original body is moved into a new private method and the method is rewritten to call the aspect around it. Simplified, the `Add` method above becomes:

```csharp
public int Add(int a, int b)
{
    var aspect = new LogAttribute();           // a new aspect instance for every call (or a shared one, see Aspect instances)
    var args = new MethodExecutionArgs
    {
        Instance = this,
        Method = /* cached MethodBase of Add */,
        Arguments = new object[] { a, b }
    };

    aspect.OnEntry(args);
    int result;
    try
    {
        result = $_executor_Add(a, b);         // the original method body
    }
    catch (Exception ex)                       // only woven if the aspect overrides OnException
    {
        args.Exception = ex;
        aspect.OnException(args);
        throw;
    }

    args.ReturnValue = result;
    aspect.OnExit(args);
    return (int)args.ReturnValue;
}
```

Consequences worth knowing:

- The aspect's constructor arguments and properties from the attribute (e.g. `[Log(Level = "Debug")]`) are applied to the new instance on every call, unless the aspect holds no state and one instance is shared (see [Aspect instances](#aspect-instances)). Instance fields of an aspect are **not** shared between calls; use `args.MethodExecutionTag` to pass data from `OnEntry` to `OnExit`/`OnException`.
- The original body runs in the private method `$_executor_<Name>`, which shows up in stack traces next to the original method, and `MethodBase.GetCurrentMethod()` called in the method body returns the executor, not the original method. `[SkipLocalsInit]` and `MethodImplOptions.NoOptimization`/`AggressiveOptimization` of the method apply to the executor, the other `MethodImpl` flags (e.g. `NoInlining`) stay on the original method.
- Only the method body is guarded by `OnException`: an exception thrown by `OnEntry` or `OnExit` itself does not call `OnException` and is thrown to the caller.
- Only overridden aspect methods are called, and only the `MethodExecutionArgs` properties the aspects actually use are filled (see [Performance](#performance)).
- Async methods are woven differently, see [Async methods](#async-methods).

## Writing aspects

Derive from `OnMethodBoundaryAspect` and override any of `OnEntry`, `OnExit` and `OnException`. All three get the same [MethodExecutionArgs](src/MethodBoundaryAspect/Attributes/MethodExecutionArgs.cs) instance per call:

| Property | Content |
|---|---|
| `Instance` | the object the method is called on, `null` for static methods |
| `Method` | the called method as [MethodBase](https://learn.microsoft.com/dotnet/api/system.reflection.methodbase) |
| `Arguments` | the argument values as `object[]` (can be changed, see [Changing input arguments](#changing-input-arguments)) |
| `ReturnValue` | the return value in `OnExit` (for async methods the returned `Task`), can be overwritten |
| `Exception` | the thrown exception in `OnException` |
| `FlowBehavior` | controls what happens after the aspect method, see [FlowBehavior](#skipping-the-method-or-swallowing-exceptions-flowbehavior) |
| `MethodExecutionTag` | any object you want to pass from `OnEntry` to `OnExit`/`OnException` of the same call |

### When the aspect methods are called

| | Synchronous method | Async method |
|---|---|---|
| `OnEntry` | before the method body | when the method is called |
| `OnExit` | after the method body completed successfully (**not** after an exception) | when the method returns its `Task` to the caller, usually before the asynchronous work is done, also if it fails later |
| `OnException` | when the method body throws | when the asynchronous work fails, possibly long after `OnExit` |

Iterator methods (`yield return`, also `async IAsyncEnumerable<T>` methods) are woven like synchronous methods: the compiler moves their code into a state machine which runs while the caller enumerates the result. So `OnEntry` and `OnExit` are called when the method returns the enumerable, before any element is produced, `args.ReturnValue` is the enumerable, and `OnException` is not called for exceptions thrown during the enumeration.

### Passing data between the aspect methods: `MethodExecutionTag`

A transaction aspect creates the scope in `OnEntry` and completes or disposes it at the end:

```csharp
using System.Transactions;
using MethodBoundaryAspect.Fody.Attributes;

public sealed class TransactionScopeAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = new TransactionScope();
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        var transactionScope = (TransactionScope)args.MethodExecutionTag;
        transactionScope.Complete();
        transactionScope.Dispose();
    }

    public override void OnException(MethodExecutionArgs args)
    {
        var transactionScope = (TransactionScope)args.MethodExecutionTag;
        transactionScope.Dispose();
    }
}

public class Repository
{
    [TransactionScope]
    public void Save()
    {
        // database work isolated in the surrounding transaction
    }
}
```

Each aspect has its own `MethodExecutionTag`, even if several aspects are applied to the same method.

## Applying aspects

### Methods, classes, properties and assemblies

```csharp
[assembly: Log]                   // all methods of all types in the assembly

[Log]                             // all methods of the class (and of its nested classes)
public class OrderService
{
    [Log]                         // this method only
    public void PlaceOrder() { }

    [Log]                         // getter and setter
    public string Name { get; set; }
}
```

Class and assembly aspects also hit property getters and setters. To exclude them, annotate the aspect class with `[AspectSkipProperties(true)]`.

Not woven: aspects applied directly to an event (class and assembly aspects do hit the `add`/`remove` accessors; weaving aspects of an event is planned for 4.0), constructors, abstract and interface methods, `extern` methods, methods with a variable argument list (`__arglist`, with a build warning), compiler-generated methods and types (lambdas, local functions, the state machines of async and iterator methods) and aspect classes: classes derived from `OnMethodBoundaryAspect` (also base aspect classes) and their nested classes, so an assembly aspect doesn't call itself ([#70](https://github.com/vescon/MethodBoundaryAspect.Fody/issues/70)).

Aspects can be generic (generic attributes need C# 11): `[Validate<int>]` weaves the aspect with the type argument `int`.

### Excluding methods: `[DisableWeaving]`

`[DisableWeaving]` excludes a method, a property (getter and setter), an event (add and remove accessor), a class (including its nested classes) or a whole assembly from weaving, e.g. to skip a hot path that is hit by an assembly-wide aspect.

### Filtering by name and visibility

Aspects applied to a class or assembly can be narrowed down with regular expressions, which are matched against the namespace, the type name and the method name (property accessors are named `get_Name`/`set_Name`). Nested classes have the namespace of their outermost class, their type name is the name of the nested class itself (e.g. `Inner` for `Outer.Inner`). Generic types have their metadata name with the number of type parameters, e.g. ``Repository`1`` for `Repository<T>`:

```csharp
[assembly: Log(NamespaceFilter = @"^MyApp\.Services", TypeNameFilter = "Service$", MethodNameFilter = "^(?!get_|set_)")]
```

`AttributeTargetMemberAttributes` restricts the visibility of the methods (by default methods of any visibility are woven, `MulticastAttributes.AnyVisibility`). Methods of other visibilities are not rewritten at all. E.g. only public methods (and public property accessors):

```csharp
[assembly: Log(AttributeTargetMemberAttributes = MulticastAttributes.Public)]
```

or only public and internal methods:

```csharp
[assembly: Log(AttributeTargetMemberAttributes = MulticastAttributes.Public | MulticastAttributes.Internal)]
```

The filter is applied to the visibility of the method itself, so a public method of an internal class is public as well.

`NamespaceFilter`, `TypeNameFilter`, `MethodNameFilter` and `AttributeTargetMemberAttributes` are read **where the aspect is applied** (as named arguments of the attribute). The weaver does not run the code of your aspects, so a value set in the constructor of the aspect, or by overriding the property, is ignored. A build warning is written for each aspect which does that.

### Several aspects on one method

Without further configuration the aspects are called in the order assembly → class → method, and in declaration order on the same level. `OnExit` and `OnException` are called in reverse order.

An aspect applied on several levels runs once per application: with `[Log]` on the class and on one of its methods, that method calls `Log` twice. Remove the attribute from the method to call it once.

To define the order explicitly, use `[AspectOrderIndex]` on the assembly, class or method (the lower index runs `OnEntry` first; a method level index overrides a class level index, which overrides an assembly level index; for a nested class the index of the closest class that has one wins). If one aspect of a method has an index, all aspects of the method need a unique one:

```csharp
[AspectOrderIndex(typeof(TransactionScopeAttribute), 1)]
[AspectOrderIndex(typeof(LogAttribute), 2)]
public class OrderService
{
    [Log, TransactionScope]
    public void PlaceOrder() { }  // TransactionScope.OnEntry, Log.OnEntry, body, Log.OnExit, TransactionScope.OnExit
}
```

`[ProvideAspectRole]` and `[AspectRoleDependency]` are obsolete and will be removed in version 4, use `[AspectOrderIndex]` instead. A build warning is written for each aspect using them. They have known problems which are not fixed: a role is ignored if its aspect has no `[AspectRoleDependency]` itself, and a chain of dependencies (A after B, B after C) is rejected with an `InvalidAspectConfigurationException`. `OnMethodBoundaryAspect.CompileTimeValidate` is obsolete as well: the weaver never calls it (it does not run the code of aspects), and it will be removed in version 4.

## Changing the method behavior

### Changing return values

Set `args.ReturnValue` in `OnExit`:

```csharp
using System;
using System.Linq;
using MethodBoundaryAspect.Fody.Attributes;

public sealed class IndentAttribute : OnMethodBoundaryAspect
{
    public override void OnExit(MethodExecutionArgs args)
    {
        args.ReturnValue = string.Concat(args.ReturnValue.ToString().Split('\n').Select(line => "  " + line));
    }
}

public class Program
{
    [Indent]
    public static string GetLogs() => "Detailed Log 1\nDetailed Log 2";

    public static void Main()
    {
        Console.WriteLine(GetLogs()); // "  Detailed Log 1\n  Detailed Log 2"
    }
}
```

For async methods, `args.ReturnValue` is the returned `Task` and can be replaced by a continuation, see [Async methods](#async-methods).

### Changing input arguments

Change the elements of `args.Arguments` in `OnEntry`. The aspect class has to be annotated with `[AllowChangingInputArguments]`, because the weaver has to generate additional code to pass the changed values to the method (unnecessary and slower for aspects which don't change them). `ref` and `out` values are written back to the caller. An element of a value type set to `null` passes the default value of the type. A changed `in` (or `ref readonly`) argument is passed to the method, but not written back, because the variable of the caller is read only for the method. Works for async methods as well: the changed values are passed to the method when it starts.

```csharp
using System;
using MethodBoundaryAspect.Fody.Attributes;

[AllowChangingInputArguments]
public sealed class InputArgumentIncrementorAttribute : OnMethodBoundaryAspect
{
    public int Increment { get; set; }

    public override void OnEntry(MethodExecutionArgs args)
    {
        var arguments = args.Arguments;
        for (var i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] is int value)
                arguments[i] = value + Increment;
        }
    }
}

public class Program
{
    public static void Main()
    {
        MethodByValue(10);                              // prints 11

        var value = 10;
        MethodByRef(ref value);                         // prints 20
        Console.WriteLine("after method call: " + value); // prints 20
    }

    [InputArgumentIncrementor(Increment = 1)]
    public static void MethodByValue(int i) => Console.WriteLine(i);

    [InputArgumentIncrementor(Increment = 10)]
    public static void MethodByRef(ref int i) => Console.WriteLine(i);
}
```

### Skipping the method or swallowing exceptions: `FlowBehavior`

| Set in | `args.FlowBehavior` | Effect |
|---|---|---|
| `OnEntry` | `FlowBehavior.Return` | the method body (and the `OnEntry` of the following aspects) is skipped, the method returns `args.ReturnValue` |
| `OnException` | `FlowBehavior.Continue` or `FlowBehavior.Return` | the exception is swallowed, the method returns `args.ReturnValue` |
| `OnException` | `FlowBehavior.Default` or `FlowBehavior.RethrowException` | the exception is rethrown (default) |

If no aspect sets `args.ReturnValue`, the method returns the default value of its return type (a completed task for a method returning `Task` or `Task<T>`, not `null`). A method with a `ref` return has no default value to return: skipping its body or swallowing its exception throws a `NotSupportedException` (with the original exception as inner exception). With several aspects, the `OnExit` of the aspects ordered before the aspect that set `FlowBehavior` is still called. For async methods see [below](#flowbehavior-in-async-methods).

```csharp
using System.Collections.Concurrent;
using MethodBoundaryAspect.Fody.Attributes;

public sealed class CacheAttribute : OnMethodBoundaryAspect
{
    private static readonly ConcurrentDictionary<string, object> Cache = new ConcurrentDictionary<string, object>();

    public override void OnEntry(MethodExecutionArgs args)
    {
        var key = args.Method.Name + string.Join(",", args.Arguments);
        if (Cache.TryGetValue(key, out var cached))
        {
            args.ReturnValue = cached;
            args.FlowBehavior = FlowBehavior.Return;   // skip the method body
        }
        args.MethodExecutionTag = key;
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        Cache[(string)args.MethodExecutionTag] = args.ReturnValue;
    }
}
```

## Async methods

Aspects work on `async` methods returning `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` and `void`, but the timing differs from synchronous methods (see [When the aspect methods are called](#when-the-aspect-methods-are-called)). Applied to

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

So, unlike for synchronous methods, `OnExit` is called whether or not `OnException` is called. If `OnException` should only handle exceptions thrown before the method returned, track it with the `MethodExecutionTag`:

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

### Task-like types with their own async method builder

`OnException` is woven into the catch block of the compiler-generated state machine, which completes the async method builder of the compiler (`AsyncTaskMethodBuilder`, `AsyncValueTaskMethodBuilder`, `AsyncVoidMethodBuilder`, ...). Methods returning a task-like type with its own builder (e.g. `UniTask`) can only be woven if their aspects use `OnEntry` and `OnExit` only. A method which also has an aspect with `OnException` is not changed at all, and a build warning is written ("... is not weaved"). Exclude the method from weaving with `[DisableWeaving]` to remove the warning.

### FlowBehavior in async methods

- `FlowBehavior.Return` in `OnEntry`: `args.ReturnValue` is what the method returns, so it has to be a task, e.g. `args.ReturnValue = Task.FromResult(42);`. If no aspect sets it, the method returns a completed task (with the default value as result for `Task<T>`).
- `FlowBehavior.Continue` in `OnException`: the returned task completes successfully with `args.ReturnValue` as its result (the value, not a task), e.g. `args.ReturnValue = 0;` for a `Task<int>`. If no aspect sets it, the result is the default value. `args.ReturnValue` is set to `null` before `OnException` is called, the task returned to the caller (provided to `OnExit`) is never the result.
- An exception thrown in `OnException` (e.g. to wrap the original exception) faults the returned task with that exception, like it is thrown to the caller of a synchronous method. The `OnException` of the remaining aspects is not called.

## Ref structs (`Span<T>`, `ReadOnlySpan<T>`, ...)

Ref structs cannot be boxed, so their values cannot be passed to the aspect via `MethodExecutionArgs`. Methods using them are still woven, but:

- ref struct arguments (also `ref`, `in` and `out`) are `null` in `args.Arguments`
- a ref struct return value is `null` in `args.ReturnValue`
- the instance of a method declared in a `ref struct` is `null` in `args.Instance`
- changes to these values by the aspect are ignored (changed arguments, overwritten return value). If the aspect skips the method body (`FlowBehavior.Return`) or swallows an exception (`FlowBehavior.Continue`), the method returns the default value (e.g. an empty `Span<T>`)

```csharp
[Log]
public class Parser
{
    // args.Arguments is [null, 42] in OnEntry
    public bool TryParse(ReadOnlySpan<char> text, int maxLength) => text.Length <= maxLength;

    // args.ReturnValue is null in OnExit
    public Span<byte> Slice(byte[] buffer) => buffer.AsSpan(1);
}
```

A build warning is written for each woven method using ref structs. Suppress these warnings with `SuppressRefStructWarnings="true"` (see [Configuration reference](#configuration-reference)), or exclude the method from weaving with `[DisableWeaving]`.

Unmanaged pointers (`int*`, `void*`, function pointers) cannot be boxed either and are handled the same way, without a warning: they are `null` in `args.Arguments` and `args.ReturnValue` and passed unchanged to the method. `IntPtr` and `UIntPtr` are ordinary values and are provided.

## Performance

The aspect calls are woven into the IL at build time, so there is no reflection and no proxy at runtime. On top of that the weaver avoids work wherever it can prove that an aspect doesn't need it:

| Optimization | What it saves | Opt-out |
|---|---|---|
| [Only the `MethodExecutionArgs` properties that the aspects use are provided](#providing-only-what-is-used) | creating and boxing the arguments array, boxing the return value (and reading it back), setting the instance, looking up the method | `DisableExecutionArgsOptimization` |
| [Aspects without state are created once](#aspect-instances) | the allocation of the aspect on every call | `DisableAspectInstanceReuse` |
| [The method info is loaded once](#method-info-of-args-method) | the reflection call that resolves `args.Method`, on every call | - |
| Only the aspect methods you override are called | calls of empty base methods, and the `try`/`catch` for `OnException` if no aspect overrides it | - |
| The original body runs in a method marked `AggressiveInlining` | the JIT can inline it again, so there is usually no additional stack frame | - |
| `MethodExecutionTag` is only saved and restored with several aspects on a method | a local variable and two assignments per aspect | - |
| Async methods get no wrapper method | no additional call level, the aspect state lives in the compiler-generated state machine | - |

Measured overhead of an aspect with `OnEntry` and `OnExit` compared to the same call without an aspect: see [Measured overhead](#measured-overhead).

### Providing only what is used

Filling all `MethodExecutionArgs` properties on every call costs time and memory: the arguments are boxed into a new `object[]`, the return value is boxed, and the `MethodBase` is loaded. Therefore the weaver analyzes the IL of the aspects' `OnEntry`, `OnExit` and `OnException` methods, and values that no aspect of a method uses are not provided:

| Not used by any aspect of the method | Not done at runtime |
|---|---|
| `args.Arguments` (read) | the arguments array is not created, the arguments are not boxed. Still done for `[AllowChangingInputArguments]` aspects |
| `args.ReturnValue` (read or write) | the return value is not boxed into `args.ReturnValue` before `OnExit` |
| `args.ReturnValue` (write) | the return value is not read back (unboxed) from `args.ReturnValue` after the aspect calls |
| `args.Method` | the `MethodBase` is not looked up |
| `args.Instance` | the instance is not set (and not boxed for structs) |

For example, only `args.Method` is provided for this aspect:

```csharp
public sealed class TimingAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = Stopwatch.StartNew();
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        var stopwatch = (Stopwatch)args.MethodExecutionTag;
        Console.WriteLine($"{args.Method.Name} took {stopwatch.ElapsedMilliseconds} ms");
    }
}
```

The analysis is conservative. If `args` is used in any other way than reading or writing its properties (for example, passed to a logger, stored in a field, or captured by a lambda), the aspect gets all properties. Calls to non-virtual methods of the aspect and its base classes are followed, e.g. `base.OnEntry(args)` or a private helper. When several aspects are applied to a method, a property is provided if any of them uses it. Aspects whose assembly can only be resolved as a reference assembly are never optimized.

The analysis runs when the project using the aspect is woven. If that project is not rebuilt when the aspect's assembly is updated (e.g. an aspect loaded from a plugin), a new aspect version could access properties that were not provided and are `null`. Disable the optimization for such aspects, see [Configuration reference](#configuration-reference).

### Method info of `args.Method`

`args.Method` is not resolved by reflection on every call. The weaver generates the internal static class `OnMethodBoundaryAspectCompile.MethodInfos` with a field and a getter for each woven method. The getter loads the `MethodBase` the first time it is called (from the metadata token of the method, with the declaring type for methods of generic types and for generic methods) and returns it from the field afterwards. Each method has its own getter, so a method whose `MethodBase` cannot be loaded does not affect the others.

### Aspect instances

An aspect that holds no state is created once and shared by all calls, instead of being created for every call. The weaver decides this by analyzing the IL of the aspect, and is conservative: an aspect is only shared if it can't be told from a new instance, i.e. all of these hold for the aspect and its base aspects:

- the instance fields are primitives, enums, `string` or `Type` (no collections, arrays or other objects), and only the constructor writes them
- the constructors only store their arguments and constants in fields (no counting of instances, no reading of a configuration, ...), and the property setters only store the value
- no method of the aspect calls a setter, takes the address of a field, captures `this` in a lambda or hands `this` to other code (calling its own methods and `GetType()` is fine)
- no finalizer, and the aspect and its constructors are accessible from the generated code (a `private` nested aspect is created for every call)

Other aspects behave as before, a new instance for every call. Use `args.MethodExecutionTag` for data of a call, as the instance is shared between concurrent calls.

The weaver decides when the project is woven, like for the [`MethodExecutionArgs` optimization](#performance). If the assembly of the aspect is replaced by a version holding state and the project using it is not rebuilt, the new version is still shared. Rebuild the projects using the aspect after changing it. Sharing is disabled in the [configuration](#configuration-reference) for all aspects, or for one aspect if it causes problems.

### Measured overhead

Overhead of an aspect with `OnEntry` and `OnExit` compared to the same call without an aspect ([benchmark](src/MethodBoundaryAspect.Fody.Benchmark/Program.cs), .NET 8, BenchmarkDotNet short run). The allocations are what the call allocates in total (the call without an aspect allocates nothing, the generic one 24 B); the times of a short run vary by some ns between machines and runs:

| Aspect | Static method: time | Static method: allocated | Open generic method: time | Open generic method: allocated |
|---|---:|---:|---:|---:|
| uses no property | +2.8 ns | 72 B | +4.8 ns | 96 B |
| uses no property, created for every call (`DisableAspectInstanceReuse`) | +3.3 ns | 120 B | - | - |
| uses `args.Method` | +4.1 ns | 72 B | - | - |
| all properties provided (`DisableExecutionArgsOptimization`) | +10.9 ns | 152 B | +9.6 ns | 128 B |

To reproduce the numbers (the project weaves with the local weaver, so build it first; run it from its folder, BenchmarkDotNet needs to find exactly one project file):

```
dotnet build -c Release src/MethodBoundaryAspect.Fody
cd src/MethodBoundaryAspect.Fody.Benchmark
dotnet run -c Release -f net10.0 -- --filter "*" --job short
```

The project targets `net8.0` and `net10.0`. The same benchmark on .NET 10 (default BenchmarkDotNet job, baseline 20.9 ns, the generic call without an aspect allocates nothing there):

| Aspect | Static method: time | Static method: allocated | Open generic method: time | Open generic method: allocated |
|---|---:|---:|---:|---:|
| uses no property | +2.8 ns | 72 B | +2.7 ns | 72 B |
| uses no property, created for every call (`DisableAspectInstanceReuse`) | +0.0 ns | 0 B | - | - |
| uses `args.Method` | +4.0 ns | 72 B | - | - |
| all properties provided (`DisableExecutionArgsOptimization`) | +11.3 ns | 152 B | +8.3 ns | 128 B |

The .NET 10 JIT removes the allocation of an aspect that is created for every call when the aspect does nothing and doesn't leak (escape analysis), so the numbers of that row only apply to such empty aspects.

### Tips for hot paths

- Use only the `args` properties you need, and don't pass `args` to other code (see above). Providing `Arguments` and `ReturnValue` costs the most: an array and boxing.
- Keep aspects free of state: use `args.MethodExecutionTag` for the data of a call. Then the aspect is [shared](#aspect-instances).
- Override only the aspect methods you need. An aspect with `OnEntry` only has no `try`/`catch` and no `OnExit` call.
- `[AllowChangingInputArguments]` makes the weaver create the arguments array and copy the values back, use it only for aspects that change arguments.
- Weave less: [filter](#filtering-by-name-and-visibility) class and assembly aspects by namespace, type, method name or visibility, and exclude hot paths with [`[DisableWeaving]`](#excluding-methods-disableweaving).

## Configuration reference

All settings are optional and go into the `MethodBoundaryAspect` element of `FodyWeavers.xml`:

```xml
<Weavers>
  <MethodBoundaryAspect SuppressRefStructWarnings="true">
    <DisableExecutionArgsOptimization Aspect="MyCompany.Logging.LogAttribute" />
    <DisableExecutionArgsOptimization Aspect="MyCompany.Plugins.PluginAttribute" />
    <DisableAspectInstanceReuse Aspect="MyCompany.Logging.CachingAttribute" />
  </MethodBoundaryAspect>
</Weavers>
```

| Setting | Effect |
|---|---|
| `SuppressRefStructWarnings="true"` | no build warnings for woven methods using [ref structs](#ref-structs-spant-readonlyspant-) |
| `<DisableExecutionArgsOptimization Aspect="..." />` | always provide all `MethodExecutionArgs` properties for this aspect (full type name, nested types as `Namespace.Outer+Inner`), see [Performance](#performance). A build warning is written if the aspect is not applied to any woven method |
| `DisableExecutionArgsOptimization="true"` | the same for all aspects |
| `<DisableAspectInstanceReuse Aspect="..." />` | create this aspect for every call instead of sharing one instance, see [Aspect instances](#aspect-instances) (full type name, nested types as `Namespace.Outer+Inner`). A build warning is written if the aspect is not applied to any woven method |
| `DisableAspectInstanceReuse="true"` | the same for all aspects |

## Build warnings and errors

The weaver writes a build warning instead of failing if it leaves something as it is or ignores a setting.

| Warning | Cause | What to do |
|---|---|---|
| `Method '...' is not weaved: ...` | the method can't be woven: an async method of a task-like type with its own builder and an `OnException` aspect, a method with a variable argument list (`__arglist`) | [`[DisableWeaving]`](#excluding-methods-disableweaving) on the method removes the warning |
| `Method '...' uses ref struct values ...` | the method has `Span<T>` or other [ref structs](#ref-structs-spant-readonlyspant-) in its signature | `SuppressRefStructWarnings="true"` or `[DisableWeaving]` |
| `Aspect '...' sets NamespaceFilter ...` | a filter is set in the constructor of the aspect or by overriding the property, the weaver ignores it | set it [where the aspect is applied](#filtering-by-name-and-visibility) |
| `Aspect '...' uses [ProvideAspectRole] or [AspectRoleDependency] ...` | the obsolete roles are used | use [`[AspectOrderIndex]`](#several-aspects-on-one-method) |
| `Aspect '...' configured in DisableExecutionArgsOptimization/DisableAspectInstanceReuse ... was not found` | the aspect named in `FodyWeavers.xml` is not applied to any woven method | use the full type name, or remove the entry |

An invalid setup of an aspect (e.g. an invalid regular expression in a filter, two aspects of a method with the same order index) fails the build with a message that names the aspect or the method, e.g. `Aspect '...' has an invalid regular expression in ...` or `Method '...' could not be weaved: ...`.

## Upgrading to version 3

Version 3 only provides the `MethodExecutionArgs` properties that the aspects of a method use (see [Performance](#performance)). Which properties are used is determined when the project using the aspect is woven. This changes the runtime behavior in these cases:

- **The aspect code changes without the woven project being woven again.** If the aspect's assembly is replaced by a newer version (e.g. a plugin, a separately deployed library, or a binding redirect) and the project using the aspect is not rebuilt, the new aspect version gets `null` for the properties the old version didn't use (`Arguments`, `Method`, `Instance`, `ReturnValue`), and a `ReturnValue` it sets is ignored. Rebuild all projects using the aspect after changing it, or [disable the optimization](#configuration-reference) for this aspect.
- **The method body is skipped without a return value.** If an aspect skips the method body (`FlowBehavior.Return` in `OnEntry`, or `FlowBehavior.Continue`/`FlowBehavior.Return` in `OnException`) and no aspect sets `args.ReturnValue`, the method returns the default value of its return type. Version 2 threw a `NullReferenceException` for value types in this case.

Also changed:

- **`MulticastAttributes.AnyVisibility` weaves methods of all visibilities** ([#132](https://github.com/vescon/MethodBoundaryAspect.Fody/issues/132)). In version 2 it had the same value as `MulticastAttributes.Public`, so aspects with `AttributeTargetMemberAttributes = MulticastAttributes.AnyVisibility` only hit public methods. Now private, protected and internal methods are woven as well, like for aspects without `AttributeTargetMemberAttributes`. Use `MulticastAttributes.Public` to keep the previous behavior. An explicit `MulticastAttributes.Default` also weaves all visibilities now (version 2: no methods at all). The default value of the `AttributeTargetMemberAttributes` property (as read in aspect code at runtime) is `MulticastAttributes.AnyVisibility`, i.e. 126; in version 2 it was 64, the value of `Public`.
- **An exception thrown in `OnException` of an async method faults the returned task** ([#5](https://github.com/vescon/MethodBoundaryAspect.Fody/issues/5)). In version 2 it escaped the compiler-generated state machine: it was thrown synchronously to the caller if the method had not awaited anything yet (and the task never completed), otherwise it was unhandled on the thread pool and terminated the process.
- **Aspect classes are never woven** ([#70](https://github.com/vescon/MethodBoundaryAspect.Fody/issues/70)). Version 2 only skipped weaving an aspect into its own class. An assembly or class aspect was still woven into the other aspects of the assembly, into base aspect classes and into classes nested in an aspect. That caused a `StackOverflowException` when two aspects were woven into each other, or when the aspect's `OnEntry` was inherited from a base aspect in the same assembly. Now no aspect is woven into a class derived from `OnMethodBoundaryAspect` or its nested classes, also if it is applied directly to one of their methods.
- **Class and assembly aspects no longer hit the state machines of async and iterator methods** ([#66](https://github.com/vescon/MethodBoundaryAspect.Fody/issues/66)). In version 2 they were also woven into the compiler-generated `MoveNext` and `SetStateMachine` methods (and the other members of iterator state machines), so the aspect was additionally called with `args.Method.Name == "MoveNext"` each time an async method started or resumed after an `await`, and each time an iterator was advanced. The async or iterator method itself is still woven.

The configuration in `FodyWeavers.xml` is unchanged; the optimization is enabled by default.

### Changed in 3.4.0

Fixes of cases that did not work, listed because the behavior of existing code can change:

- **Init accessors, records and `in` parameters of virtual methods can be woven.** They crashed at runtime (`InvalidProgramException`, `AccessViolationException`).
- **A skipped method body or a swallowed exception returns the default value** if no aspect sets `args.ReturnValue`, instead of throwing a `NullReferenceException` for value types (also for async methods, where the swallowed exception could complete the task with an `InvalidCastException`).
- **`[AllowChangingInputArguments]`:** `in` arguments are not written back to the caller anymore, and `ref` arrays of enums (e.g. `ref Color[]`) are written back as references.
- **Methods with more than 255 arguments** passed the wrong arguments to the method from the 256th argument on.
- **The method infos of `args.Method` are loaded on the first call of each method**, not all at once on the first call of any woven method, and a method that can't be loaded doesn't break the others. The cache fields are numbered instead of named by a SHA256 hash, which failed in .NET Framework MSBuild and Visual Studio with the FIPS mode of Windows enabled.
- **An invalid regular expression in `NamespaceFilter`, `TypeNameFilter` or `MethodNameFilter`** fails the build with a message that names the aspect.
- The XML documentation of the attributes is part of the package, so IntelliSense shows it.

### Changed in 3.5.0

Mostly fixes of cases that did not work, listed because the behavior of existing code can change:

- **`[DisableWeaving]` on a property or an event is honored.** It was ignored there, a class or assembly aspect still hit the getter, setter, `add` and `remove` accessors.
- **A method that an aspect skips or whose exception it swallows returns a completed task**, not `null`, if it returns `Task` or `Task<T>` and no aspect sets `args.ReturnValue`.
- **A `ref` returning method throws a `NotSupportedException`** if its exception is swallowed (`FlowBehavior.Continue` in `OnException`), like it already did for `FlowBehavior.Return` in `OnEntry`. It returned a null reference before, which failed in the caller as soon as it used it.
- **Methods with a variable argument list (`__arglist`) are not woven** and a build warning is written. They were woven, but lost the variable arguments.
- **`[AllowChangingInputArguments]` works for async methods.** The changed arguments were ignored.
- **`null` for a value type argument passes the default value** instead of throwing a `NullReferenceException`.
- **Aspects without state are created once** instead of for every call, see [Aspect instances](#aspect-instances). Opt-out: `DisableAspectInstanceReuse`.
- **The executor method keeps `[SkipLocalsInit]`, `MethodImplOptions.NoOptimization` and `AggressiveOptimization`** of the woven method.
- **The attributes `[DisableWeaving]`, `[AspectOrderIndex]`, `[AspectSkipProperties]` and `[AllowChangingInputArguments]` can only be applied where the weaver reads them** (and not twice). Applied anywhere else, they were ignored before and the code doesn't compile now.
- **`[AspectOrderIndex]` of an assembly and of an enclosing class** failed the build for the methods of a nested class. The closest level wins now.
- **Async methods whose aspects override none of the aspect methods** are left alone.
- Fixed: generic aspects (`[Aspect<int>]`) crashed at runtime, `[AspectOrderIndex]` on a nested class failed the build, a ref struct warning was written for methods the weaver does not change, and an invalid aspect setup was reported as an unhandled exception without the method.

## Contributing

Issues and pull requests are welcome, feel free to [fork](https://github.com/vescon/MethodBoundaryAspect.Fody/fork) the repository.

```
dotnet build --configuration Release src/MethodBoundaryAspect.Fody.sln
dotnet test src/MethodBoundaryAspect.Fody.UnitTests.NetFramework --configuration Release --no-build
dotnet test src/MethodBoundaryAspect.Fody.UnitTests.NetCore --configuration Release --no-build
dotnet test src/MethodBoundaryAspect.Fody.RuntimeTests --configuration Release --no-build
```

`RuntimeTests` builds a project with the weaver from this repository and runs the woven code on .NET Framework 4.6.2 and 4.8 and .NET 8, 9 and 10.
