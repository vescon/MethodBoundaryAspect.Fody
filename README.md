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
    var aspect = new LogAttribute();           // a new aspect instance for every call
    var args = new MethodExecutionArgs
    {
        Instance = this,
        Method = /* cached MethodBase of Add */,
        Arguments = new object[] { a, b }
    };

    aspect.OnEntry(args);
    try
    {
        var result = $_executor_Add(a, b);     // the original method body
        args.ReturnValue = result;
        aspect.OnExit(args);
        return (int)args.ReturnValue;
    }
    catch (Exception ex)                       // only woven if the aspect overrides OnException
    {
        args.Exception = ex;
        aspect.OnException(args);
        throw;
    }
}
```

Consequences worth knowing:

- The aspect's constructor arguments and properties from the attribute (e.g. `[Log(Level = "Debug")]`) are applied to the new instance on every call. Instance fields of an aspect are **not** shared between calls; use `args.MethodExecutionTag` to pass data from `OnEntry` to `OnExit`/`OnException`.
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

Not woven: constructors, abstract and interface methods, `extern` methods, compiler-generated methods (lambdas, local functions) and the methods of the aspect class itself.

### Excluding methods: `[DisableWeaving]`

`[DisableWeaving]` excludes a method, a class (including its nested classes) or a whole assembly from weaving, e.g. to skip a hot path that is hit by an assembly-wide aspect.

### Filtering by name and visibility

Aspects applied to a class or assembly can be narrowed down with regular expressions, which are matched against the namespace, the type name and the method name (property accessors are named `get_Name`/`set_Name`):

```csharp
[assembly: Log(NamespaceFilter = @"^MyApp\.Services", TypeNameFilter = "Service$", MethodNameFilter = "^(?!get_|set_)")]
```

`AttributeTargetMemberAttributes` restricts the visibility of the methods, e.g. only public and internal methods:

```csharp
[assembly: Log(AttributeTargetMemberAttributes = MulticastAttributes.Public | MulticastAttributes.Internal)]
```

### Several aspects on one method

Without further configuration the aspects are called in the order assembly → class → method, and in declaration order on the same level. `OnExit` and `OnException` are called in reverse order.

To define the order explicitly, use `[AspectOrderIndex]` on the assembly, class or method (the lower index runs `OnEntry` first; a method level index overrides a class level index, which overrides an assembly level index). If one aspect of a method has an index, all aspects of the method need a unique one:

```csharp
[AspectOrderIndex(typeof(TransactionScopeAttribute), 1)]
[AspectOrderIndex(typeof(LogAttribute), 2)]
public class OrderService
{
    [Log, TransactionScope]
    public void PlaceOrder() { }  // TransactionScope.OnEntry, Log.OnEntry, body, Log.OnExit, TransactionScope.OnExit
}
```

`[ProvideAspectRole]` and `[AspectRoleDependency]` are obsolete, use `[AspectOrderIndex]` instead.

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

Change the elements of `args.Arguments` in `OnEntry`. The aspect class has to be annotated with `[AllowChangingInputArguments]`, because the weaver has to generate additional code to pass the changed values to the method (unnecessary and slower for aspects which don't change them). `ref` and `out` values are written back to the caller. Not supported for async methods.

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

If no aspect sets `args.ReturnValue`, the method returns the default value of its return type. With several aspects, the `OnExit` of the aspects ordered before the aspect that set `FlowBehavior` is still called. For async methods see [below](#flowbehavior-in-async-methods).

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

### FlowBehavior in async methods

- `FlowBehavior.Return` in `OnEntry`: `args.ReturnValue` is what the method returns, so it has to be a task, e.g. `args.ReturnValue = Task.FromResult(42);`. Otherwise the method returns `null` instead of a task.
- `FlowBehavior.Continue` in `OnException`: the returned task completes successfully with `args.ReturnValue` as its result (the value, not a task), e.g. `args.ReturnValue = 0;` for a `Task<int>`.

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

## Performance

Filling all `MethodExecutionArgs` properties on every call costs time and memory: the arguments are boxed into a new `object[]`, the return value is boxed, and the `MethodBase` is looked up (by reflection for open generic methods). Therefore the weaver analyzes the IL of the aspects' `OnEntry`, `OnExit` and `OnException` methods, and values that no aspect of a method uses are not provided:

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

Overhead of an aspect with `OnEntry` and `OnExit` compared to the same call without an aspect ([benchmark](src/MethodBoundaryAspect.Fody.Benchmark/Program.cs), .NET 8, BenchmarkDotNet short run):

| Aspect | Static method: time | Static method: allocated | Open generic method: time | Open generic method: allocated |
|---|---:|---:|---:|---:|
| uses no property | +14 ns | 120 B | +12 ns | 120 B |
| uses `args.Method` | +12 ns | 120 B | - | - |
| all properties provided (optimization disabled) | +35 ns | 200 B | +122 ns | 248 B |

The analysis is conservative. If `args` is used in any other way than reading or writing its properties (for example, passed to a logger, stored in a field, or captured by a lambda), the aspect gets all properties. Calls to non-virtual methods of the aspect and its base classes are followed, e.g. `base.OnEntry(args)` or a private helper. When several aspects are applied to a method, a property is provided if any of them uses it. Aspects whose assembly can only be resolved as a reference assembly are never optimized.

The analysis runs when the project using the aspect is woven. If that project is not rebuilt when the aspect's assembly is updated (e.g. an aspect loaded from a plugin), a new aspect version could access properties that were not provided and are `null`. Disable the optimization for such aspects, see [Configuration reference](#configuration-reference).

## Configuration reference

All settings are optional and go into the `MethodBoundaryAspect` element of `FodyWeavers.xml`:

```xml
<Weavers>
  <MethodBoundaryAspect SuppressRefStructWarnings="true">
    <DisableExecutionArgsOptimization Aspect="MyCompany.Logging.LogAttribute" />
    <DisableExecutionArgsOptimization Aspect="MyCompany.Plugins.PluginAttribute" />
  </MethodBoundaryAspect>
</Weavers>
```

| Setting | Effect |
|---|---|
| `SuppressRefStructWarnings="true"` | no build warnings for woven methods using [ref structs](#ref-structs-spant-readonlyspant-) |
| `<DisableExecutionArgsOptimization Aspect="..." />` | always provide all `MethodExecutionArgs` properties for this aspect (full type name, nested types as `Namespace.Outer+Inner`), see [Performance](#performance). A build warning is written if the aspect is not applied to any woven method |
| `DisableExecutionArgsOptimization="true"` | the same for all aspects |

## Upgrading to version 3

Version 3 only provides the `MethodExecutionArgs` properties that the aspects of a method use (see [Performance](#performance)). Which properties are used is determined when the project using the aspect is woven. This changes the runtime behavior in these cases:

- **The aspect code changes without the woven project being woven again.** If the aspect's assembly is replaced by a newer version (e.g. a plugin, a separately deployed library, or a binding redirect) and the project using the aspect is not rebuilt, the new aspect version gets `null` for the properties the old version didn't use (`Arguments`, `Method`, `Instance`, `ReturnValue`), and a `ReturnValue` it sets is ignored. Rebuild all projects using the aspect after changing it, or [disable the optimization](#configuration-reference) for this aspect.
- **The method body is skipped without a return value.** If an aspect skips the method body (`FlowBehavior.Return` in `OnEntry`, or `FlowBehavior.Continue`/`FlowBehavior.Return` in `OnException`) and no aspect sets `args.ReturnValue`, the method returns the default value of its return type. Version 2 threw a `NullReferenceException` for value types in this case.

The configuration in `FodyWeavers.xml` is unchanged; the optimization is enabled by default.

## Contributing

Issues and pull requests are welcome, feel free to [fork](https://github.com/vescon/MethodBoundaryAspect.Fody/fork) the repository.

```
dotnet build --configuration Release src/MethodBoundaryAspect.Fody.sln
dotnet test src/MethodBoundaryAspect.Fody.UnitTests.NetFramework --configuration Release --no-build
dotnet test src/MethodBoundaryAspect.Fody.UnitTests.NetCore --configuration Release --no-build
dotnet test src/MethodBoundaryAspect.Fody.RuntimeTests --configuration Release --no-build
```

`RuntimeTests` builds a project with the weaver from this repository and runs the woven code on .NET Framework 4.6.2 and 4.8 and .NET 8, 9 and 10.
