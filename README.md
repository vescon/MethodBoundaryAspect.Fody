[![CI](https://github.com/vescon/MethodBoundaryAspect.Fody/actions/workflows/main.yaml/badge.svg?branch=master)](https://github.com/vescon/MethodBoundaryAspect.Fody/actions/workflows/main.yaml)
[![NuGet](https://img.shields.io/nuget/v/MethodBoundaryAspect.Fody.svg)](https://www.nuget.org/packages/MethodBoundaryAspect.Fody/)
[![License: MIT](https://img.shields.io/github/license/vescon/MethodBoundaryAspect.Fody.svg)](LICENSE)

# MethodBoundaryAspect.Fody

Run your own code when a method starts, ends or throws, by putting an attribute on it:

```csharp
[Log]
public int Add(int a, int b) => a + b;
```

MethodBoundaryAspect.Fody is a [Fody](https://github.com/Fody/Fody) weaver: the calls to your aspect are woven into the IL of the method **at build time**. There is no runtime proxy, no dependency injection container and no interface or virtual method required. It works for static, private, generic and async methods of any class, on .NET Framework 4.6.2+ and .NET (Core) / .NET Standard 2.0+.

Typical aspects: logging, measuring execution time, transaction handling, exception wrapping, showing a wait cursor, argument validation, caching.

The overhead is small: the weaver only provides what your aspects use, loads the method info once, and creates aspects without state once instead of for every call, see [Performance](docs/performance.md).

> Upgrading from version 2? See [Upgrading to version 3](docs/upgrading-to-version-3.md).

## Why use it?

Logging, timing, transactions or exception handling are repeated in many methods. With an aspect you write that code once and mark the methods with an attribute: the methods stay readable and the code cannot be forgotten in one of them.

- **Compared to runtime proxies and decorators** (e.g. Castle DynamicProxy, or a wrapper class per service): they need an interface or virtual methods and an object that was created through a container. Here any method of any class is woven, including static and private ones, without any change to how the object is created.
- **Compared to commercial AOP frameworks** (e.g. PostSharp, Metalama): they offer more, e.g. aspects beyond method boundaries and build-time validation. MethodBoundaryAspect.Fody is MIT licensed and limited to what happens at the boundaries of a method, see [Limitations](#limitations).

## Contents

- [Quickstart](#quickstart)
- [At a glance](#at-a-glance)
- [Three things to know first](#three-things-to-know-first)
- [How it works](#how-it-works)
- [Writing aspects](#writing-aspects)
- [Applying aspects](#applying-aspects)
- [Changing the method behavior](#changing-the-method-behavior)
- [Async methods](#async-methods)
- [Limitations](#limitations)
- [Performance](#performance)
- [Troubleshooting](#troubleshooting)
- [More documentation](#more-documentation): async methods, ref structs, performance, configuration, build warnings, upgrading
- [Contributing](#contributing)

## Quickstart

1. Add the NuGet package (it brings Fody along):

   ```
   dotnet add package MethodBoundaryAspect.Fody
   ```

2. Add a `FodyWeavers.xml` file to the project root (if it is missing, Fody creates it on the first build; check that it has the entry):

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

### Aspects in a separate library

Aspects are often shared by several projects. Put them into a library that references the `MethodBoundaryAspect.Fody` package for the base class `OnMethodBoundaryAspect`. If the library has no woven methods itself, switch the weaver off for it with `<DisableFody>true</DisableFody>` in its project file (see [the sample](Samples/HelloWorld_NetCore/HelloWorld_NetCore_Attributes/HelloWorld_NetCore_Attributes.csproj)). Every project with methods to be woven references the package as well and has its own `FodyWeavers.xml`.

## At a glance

| I want to | Use |
|---|---|
| log a call and its arguments | `OnEntry` with `args.Method` and `args.Arguments` |
| log or change the result | `OnExit` with `args.ReturnValue`, see [Changing return values](#changing-return-values) |
| measure the time | start a `Stopwatch` in `OnEntry`, keep it in `args.MethodExecutionTag`, read it in `OnExit`, see [MethodExecutionTag](#passing-data-between-the-aspect-methods-methodexecutiontag) |
| log or wrap an exception | `OnException` with `args.Exception` |
| swallow an exception | `args.FlowBehavior = FlowBehavior.Continue` in `OnException`, see [FlowBehavior](#skipping-the-method-or-swallowing-exceptions-flowbehavior) |
| skip the method, e.g. return a cached value | `args.ReturnValue = ...` and `args.FlowBehavior = FlowBehavior.Return` in `OnEntry` |
| change the arguments | the elements of `args.Arguments` in `OnEntry` and `[AllowChangingInputArguments]` on the aspect, see [Changing input arguments](#changing-input-arguments) |
| run code when an async method has finished | continue the task in `args.ReturnValue` in `OnExit`, see [Async methods](#async-methods) |
| apply an aspect to many methods | put it on the class or the assembly, see [Applying aspects](#applying-aspects) |
| leave a method out | `[DisableWeaving]`, see [Excluding methods](#excluding-methods-disableweaving) |

## Three things to know first

> [!IMPORTANT]
> 1. **Aspects are not inherited.** An aspect on a class, a virtual method or an interface does not apply to derived classes, overrides or implementations. Apply it there as well, see [Several aspects on one method](#several-aspects-on-one-method).
> 2. **`OnExit` is not called after an exception.** A synchronous method calls either `OnExit` or `OnException`. Code that has to run in both cases goes into both methods.
> 3. **`OnExit` of an async method runs when the task is returned,** usually before the asynchronous work is done, and also if that work fails later. See [Async methods](#async-methods).

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

- The aspect's constructor arguments and properties from the attribute (e.g. `[Log(Level = "Debug")]`) are applied to the new instance on every call, unless the aspect holds no state and one instance is shared (see [Aspect instances](docs/performance.md#aspect-instances)). Instance fields of an aspect are **not** shared between calls; use `args.MethodExecutionTag` to pass data from `OnEntry` to `OnExit`/`OnException`.
- The original body runs in the private method `$_executor_<Name>`, which shows up in stack traces next to the original method (the JIT may inline it, then it is missing), and `MethodBase.GetCurrentMethod()` called in the method body returns the executor, not the original method. Code in the body that looks at its caller (`Assembly.GetCallingAssembly()`) sees the woven method instead of the real caller. `[SkipLocalsInit]` and `MethodImplOptions.NoOptimization`/`AggressiveOptimization` of the method apply to the executor, the other `MethodImpl` flags (e.g. `NoInlining`) stay on the original method.
- Only the method body is guarded by `OnException`: an exception thrown by `OnEntry` or `OnExit` itself does not call `OnException` and is thrown to the caller (also for async methods, where it is thrown synchronously and not stored in the returned task).
- Only overridden aspect methods are called, and only the `MethodExecutionArgs` properties the aspects actually use are filled (see [Performance](docs/performance.md)).
- Async methods are woven differently, see [Async methods](#async-methods).

## Writing aspects

Derive from `OnMethodBoundaryAspect` and override any of `OnEntry`, `OnExit` and `OnException`. All three get the same [MethodExecutionArgs](src/MethodBoundaryAspect/Attributes/MethodExecutionArgs.cs) instance per call:

| Property | Content |
|---|---|
| `Instance` | the object the method is called on, `null` for static methods |
| `Method` | the called method as [MethodBase](https://learn.microsoft.com/dotnet/api/system.reflection.methodbase) |
| `Arguments` | the argument values as `object[]` (can be changed, see [Changing input arguments](#changing-input-arguments)) |
| `ReturnValue` | the return value in `OnExit` (for async methods the returned `Task`), can be overwritten. For a method with a `ref` return it is the value of the returned variable; setting it writes through the reference (ignored for `ref readonly`, and `null` for a value type leaves the variable unchanged) |
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

Each aspect has its own `MethodExecutionTag`, even if several aspects are applied to the same method: an aspect that did not set the tag sees `null`. For async methods read the [note on the tag](docs/async-methods.md), `OnException` can run at the same time as `OnExit`.

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

    public int Count
    {
        [Log] get;                    // the getter only
        set;
    }
}
```

The members the compiler generates for records (`Equals`, `PrintMembers`, ...), the accessors of auto-properties and field-like events and default interface methods are woven like other methods. Class and assembly aspects also hit property getters and setters. To exclude them, annotate the aspect class with `[AspectSkipProperties(true)]`; it also applies when the aspect is put directly on a property. Like `[AllowChangingInputArguments]`, it is inherited by aspects derived from the annotated aspect class.

Not woven:

- constructors and finalizers (finalizers run on the finalizer thread, where an exception of an aspect ends the process)
- abstract and interface methods and `extern` methods
- methods with a variable argument list (`__arglist`, with a build warning)
- aspects applied directly to an event (class and assembly aspects do hit the `add`/`remove` accessors; weaving aspects of an event is planned for 4.0)
- types marked `[CompilerGenerated]` (closures, the state machines of async and iterator methods, anonymous types)
- methods whose name starts with `<` (lambdas, local functions, the `<Main>$` entry point of top-level statements, `<Clone>$` of records)
- aspect classes: classes derived from `OnMethodBoundaryAspect` (also base aspect classes) and their nested classes, so an assembly aspect doesn't call itself ([#70](https://github.com/vescon/MethodBoundaryAspect.Fody/issues/70))

Aspects can be generic (generic attributes need C# 11): `[Validate<int>]` weaves the aspect with the type argument `int`.

### Excluding methods: `[DisableWeaving]`

`[DisableWeaving]` excludes a method, a property (getter and setter), an event (add and remove accessor), a class (including its nested classes) or a whole assembly from weaving, e.g. to skip a hot path that is hit by an assembly-wide aspect.

### Filtering by name and visibility

An aspect can be narrowed down with regular expressions for the namespace, the type name and the method name, and by the visibility of the methods:

```csharp
// all methods of the services, but no property accessors
[assembly: Log(NamespaceFilter = @"^MyApp\.Services", TypeNameFilter = "Service$", MethodNameFilter = "^(?!get_|set_)")]
```

```csharp
// only public methods
[assembly: Log(AttributeTargetMemberAttributes = MulticastAttributes.Public)]
```

- The regular expressions are not anchored and case sensitive: `"Service"` also matches `OrderServiceFactory`, use `^Service$` for the exact name.
- Property accessors are named `get_Name` and `set_Name`.
- Without `AttributeTargetMemberAttributes` methods of any visibility are woven.
- The filters are read **where the aspect is applied** (as named arguments of the attribute). A value set in the constructor of the aspect is ignored, with a build warning.
- To weave an aspect only in some builds (e.g. a tracing aspect only in Debug), put `[Conditional("DEBUG")]` on the aspect class.

Details: [the names the filters see, the visibility values](docs/applying-aspects.md#filtering-by-name-and-visibility) and [aspects in some configurations only](docs/applying-aspects.md#applying-an-aspect-only-in-some-configurations).

### Several aspects on one method

- **Order:** `OnEntry` is called in the order assembly → class → method, and in declaration order on the same level. `OnExit` and `OnException` are called in reverse order.
- **Aspects are not inherited:** an aspect on a class does not apply to derived classes, an aspect on a virtual method not to its overrides, and an aspect on an interface not to the classes that implement it. Apply the aspect there as well.
- **An aspect applied on several levels runs once per application:** with `[Log]` on the class and on one of its methods, that method calls `Log` twice.
- **Explicit order:** use `[AspectOrderIndex]`, the lower index runs `OnEntry` first. If one aspect of a method has an index, all aspects of the method need one.

```csharp
[AspectOrderIndex(typeof(TransactionScopeAttribute), 1)]
[AspectOrderIndex(typeof(LogAttribute), 2)]
public class OrderService
{
    [Log, TransactionScope]
    public void PlaceOrder() { }  // TransactionScope.OnEntry, Log.OnEntry, body, Log.OnExit, TransactionScope.OnExit
}
```

Details: [order, inheritance and the rules of the order index](docs/applying-aspects.md#several-aspects-on-one-method).

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
        args.ReturnValue = string.Join("\n", args.ReturnValue.ToString().Split('\n').Select(line => "  " + line));
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

For async methods, `args.ReturnValue` is the returned `Task` and can be replaced by a continuation, see [Async methods](docs/async-methods.md).

### Changing input arguments

Change the elements of `args.Arguments` in `OnEntry`. The aspect class has to be annotated with `[AllowChangingInputArguments]`: the weaver has to generate additional code to pass the changed values to the method, which is unnecessary and slower for aspects which don't change them. Aspects derived from an annotated aspect class inherit it.

- Only the elements of the array count: assigning a new array to `args.Arguments` is ignored.
- `ref` and `out` values are written back to the caller.
- An element of a value type set to `null` passes the default value of the type.
- A changed `in` (or `ref readonly`) argument is passed to the method, but not written back, because the variable of the caller is read only for the method.
- An `out` parameter the method didn't assign before it threw (and an aspect swallowed the exception) keeps the value the variable of the caller had before the call, the method works on that variable.
- It works for async methods as well: the changed values are passed to the method when it starts.

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

If no aspect sets `args.ReturnValue`, the method returns the default value of its return type (a completed task for a method returning `Task` or `Task<T>`, not `null`). A method with a `ref` return has no default value to return: skipping its body or swallowing its exception throws a `NotSupportedException` (with the original exception as inner exception). The `OnExit` of the aspects which ran `OnEntry` before the aspect that skips or swallows is called first in both cases. Details:

- `FlowBehavior` is reset to `Default` before the first `OnException`, so a `Continue` set in `OnEntry` (where it has no effect) does not swallow a later exception. It has no effect in `OnExit`.
- `FlowBehavior.Return` in `OnEntry` skips the method body and the `OnEntry` of the following aspects, and also their `OnExit`. The `OnExit` of the aspects ordered before the aspect that set it is still called.
- If an aspect swallows an exception in `OnException`, the aspects ordered before it get a call of `OnExit` (not of `OnException`).

For async methods see [below](docs/async-methods.md#flowbehavior-in-async-methods).

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

Aspects work on `async` methods returning `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` and `void`. The timing differs from synchronous methods:

- `OnEntry` runs when the method is called.
- `OnExit` runs when the method returns its `Task` to the caller, usually **before** the asynchronous work is done. `args.ReturnValue` is that task.
- `OnException` runs when the asynchronous work fails, possibly long after `OnExit` and on another thread.

To run code when the asynchronous work has finished, continue the returned task in `OnExit`:

```csharp
public override void OnExit(MethodExecutionArgs args)
{
    if (args.ReturnValue is Task task)
        task.ContinueWith(t => Console.WriteLine("Asynchronous work finished"));
}
```

The page [Async methods](docs/async-methods.md) has the details: the exact order of the calls, replacing the returned task, `FlowBehavior` in async methods, the `MethodExecutionTag` when `OnExit` and `OnException` run at the same time, and task-like types such as `UniTask`.

## Limitations

What MethodBoundaryAspect.Fody does not do, collected from the sections of this document:

- **Method boundaries only.** An aspect runs before the method body, after it, and when it throws. It cannot change code inside the method, add members or interfaces, or validate how it is used at build time.
- **Not every method is woven:** constructors, finalizers, abstract, interface and `extern` methods, methods with a variable argument list (`__arglist`), methods compiled with runtime-async (.NET 11, see [workarounds](docs/async-methods.md#runtime-async-methods-net-11)), methods of `[CompilerGenerated]` types and methods named with `<` (lambdas, local functions, the entry point of top-level statements) and the aspect classes themselves, see [Applying aspects](#methods-classes-properties-and-assemblies).
- **The code of the aspect does not run at build time.** The weaver only reads the values written where the aspect is applied: a filter set in the constructor of an aspect is ignored (with a warning), `CompileTimeValidate` is never called.
- **The aspect instance is not kept between calls:** a new one is created for every call, or one shared instance for an aspect without state, see [Aspect instances](docs/performance.md#aspect-instances).
- **Async and iterator methods have a different timing:** `OnExit` runs when the method returns its task or enumerable, see [Async methods](#async-methods) and [When the aspect methods are called](#when-the-aspect-methods-are-called). Task-like types with an own builder (e.g. `UniTask`) cannot use `OnException`.
- **Ref structs and pointers** cannot be passed to the aspect, they are `null` in `args`, see [Ref structs](docs/ref-structs.md).
- **The original body runs in a private method** `$_executor_<Name>`, which shows up in stack traces, and `MethodBase.GetCurrentMethod()` in the method body returns it, see [How it works](#how-it-works).
- **Aspects are analyzed when the project that uses them is built**, so rebuild that project after changing an aspect that lives in another assembly, see [Performance](docs/performance.md).

## Performance

The aspect calls are woven into the IL at build time, so there is no reflection and no proxy at runtime. An aspect with `OnEntry` and `OnExit` that uses no `args` property costs about **3 ns and 72 bytes** per call, providing all `args` properties costs about 11 ns and 152 bytes. The weaver keeps the overhead low by itself:

- only the `MethodExecutionArgs` properties your aspects use are provided
- an aspect without state is created once and shared by all calls
- only the aspect methods you override are called

The page [Performance](docs/performance.md) describes these optimizations, how to switch them off, tips for hot paths and the benchmark tables.

## Troubleshooting

### My aspect is not called

1. The project that contains the methods references the `MethodBoundaryAspect.Fody` package and its `FodyWeavers.xml` contains `<MethodBoundaryAspect />` (Fody creates the file on the first build if it is missing, check that the entry is there).
2. The aspect derives from `OnMethodBoundaryAspect` and overrides at least one of `OnEntry`, `OnExit` and `OnException`: methods the aspect does not override are not called, an aspect that overrides none is ignored.
3. The method is one that is woven, see [Not woven](#methods-classes-properties-and-assemblies) (constructors, abstract and `extern` methods, lambdas, the code of the aspects themselves, ...).
4. Neither the method, its class nor the assembly has [`[DisableWeaving]`](#excluding-methods-disableweaving), and the [filters](#filtering-by-name-and-visibility) of the aspect (`NamespaceFilter`, `TypeNameFilter`, `MethodNameFilter`, `AttributeTargetMemberAttributes`) match the method.
5. The attribute is not removed by the compiler, e.g. by [`[Conditional]`](docs/applying-aspects.md#applying-an-aspect-only-in-some-configurations) on the aspect class.
6. Rebuild the project and read the build output: the weaver writes [warnings](docs/build-warnings.md) for what it leaves as it is.
7. Look at the built assembly with a decompiler (IL view): a woven method creates the aspect and calls it, its original body is in the private method `$_executor_<Name>`.

### The fields of my aspect are empty or reset in every call

A new aspect instance is created for every call (or, for an aspect without state, one is shared by all calls), so instance fields do not keep values between the calls. Keep per-call data in `args.MethodExecutionTag`, and shared data in a `static` field (thread safe, the calls can run concurrently).

### A property of `args` is `null`

- `args.Instance` is `null` in a static method and in a method of a ref struct, `args.Arguments` and `args.ReturnValue` are `null` for [ref structs and pointers](docs/ref-structs.md).
- A property the aspects of a method don't use is not provided, see [Providing only what is used](docs/performance.md#providing-only-what-is-used). If the aspect was changed without rebuilding the project that uses it, rebuild that project.
- `args.Exception` is only set in `OnException`, `args.ReturnValue` only in `OnExit` (and is the returned `Task` for [async methods](#async-methods)).

## More documentation

| Page | Content |
|---|---|
| [Applying aspects: details](docs/applying-aspects.md) | the names and visibilities the filters see, aspects in some build configurations only, order and inheritance of several aspects |
| [Async methods](docs/async-methods.md) | order of the calls, replacing the returned task, `FlowBehavior`, task-like types |
| [Ref structs and pointers](docs/ref-structs.md) | what an aspect gets for `Span<T>` and other values that cannot be boxed |
| [Performance](docs/performance.md) | what the weaver optimizes, shared aspect instances, tips for hot paths, benchmark tables |
| [Configuration reference](docs/configuration.md) | the settings of `FodyWeavers.xml` |
| [Build warnings and errors](docs/build-warnings.md) | what each warning of the weaver means and what to do |
| [Upgrading to version 3](docs/upgrading-to-version-3.md) | what changed since version 2 and what is planned for version 4 |
| [Changelog](CHANGELOG.md) | all changes of each release |

## Contributing

Issues and pull requests are welcome, feel free to [fork](https://github.com/vescon/MethodBoundaryAspect.Fody/fork) the repository. See [CONTRIBUTING.md](CONTRIBUTING.md) for what a change needs, and [SECURITY.md](SECURITY.md) to report a vulnerability.

```
dotnet build --configuration Release src/MethodBoundaryAspect.Fody.sln
dotnet test src/MethodBoundaryAspect.Fody.UnitTests.NetFramework --configuration Release --no-build
dotnet test src/MethodBoundaryAspect.Fody.UnitTests.NetCore --configuration Release --no-build
dotnet test src/MethodBoundaryAspect.Fody.RuntimeTests --configuration Release --no-build
```

`RuntimeTests` builds a project with the weaver from this repository and runs the woven code on .NET Framework 4.6.2, 4.8 and 4.8.1 and .NET 8, 9, 10 and 11 (a .NET 11 SDK is needed to build it).
