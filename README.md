[![.github/workflows/main.yaml](https://github.com/vescon/MethodBoundaryAspect.Fody/actions/workflows/main.yaml/badge.svg?branch=master)](https://github.com/vescon/MethodBoundaryAspect.Fody/actions/workflows/main.yaml)
[![NuGet](https://img.shields.io/nuget/v/MethodBoundaryAspect.Fody.svg)](https://www.nuget.org/packages/MethodBoundaryAspect.Fody/)

## MethodBoundaryAspect.Fody
> A [Fody weaver](https://github.com/Fody/Fody) which allows to decorate assemblies, classes and methods and hook into method start, method end and method exceptions. Additionally you have access to useful method parameters.

You can easily write your own aspects for
- transaction handling
- logging
- measuring method execution time
- exception wrapping
- displaying wait cursor
- and much more ...

### Supported features
- Hook into method start and end
- Hook into raised exceptions in a method
- Access method information like [MethodExecutionArgs](https://github.com/vescon/MethodBoundaryAspect.Fody/blob/master/src/MethodBoundaryAspect/Attributes/MethodExecutionArgs.cs)
    - applied object instance
    - the method itself ([System.Reflection.MethodBase](https://msdn.microsoft.com/en-us/library/system.reflection.methodbase))
    - the passed method arguments
    - the thrown exception
    - some custom object, which can be set at the start of the method and accessed at the end (e.g. useful for transactions or timers)
- Apply aspects at different levels
    - globally in `AssemblyInfo.cs`
    - on class
    - on method
- Filter which methods to include in weaving, using Regex patterns
    - NamespaceFilter
    - TypeNameFilter
    - MethodNameFilter
- Change method behavior (see examples below)
    - Overwrite input arguments values (byValue and byRef) to be forwarded to the method.
        - no async support
        - requires aspect to be annotated with [AllowChangingInputArgumentsAttribute](https://github.com/vescon/MethodBoundaryAspect.Fody/blob/master/src/MethodBoundaryAspect/Attributes/AllowChangingInputArgumentsAttribute.cs)        
    - Overwrite return value to be returned from the method.
- Low overhead: only the `MethodExecutionArgs` properties used by the aspects are provided at runtime (see [Performance](#performance-only-used-methodexecutionargs-properties-are-provided))

Feel free to make a [Fork](https://github.com/vescon/MethodBoundaryAspect.Fody/fork) of this repository.

### Breaking changes in version 3

Version 3 only provides the `MethodExecutionArgs` properties that the aspects of a method use (see [Performance](#performance-only-used-methodexecutionargs-properties-are-provided)). Which properties are used is determined when the project using the aspect is weaved. This changes the runtime behavior in these cases:

- **The aspect code changes without the weaved project being weaved again.** If the aspect's assembly is replaced by a newer version (e.g. a plugin, a separately deployed library, or a binding redirect) and the project using the aspect is not rebuilt, the new aspect version gets `null` for the properties the old version didn't use (`Arguments`, `Method`, `Instance`, `ReturnValue`), and a `ReturnValue` it sets is ignored. Rebuild all projects using the aspect after changing it, or [disable the optimization](#disabling-the-optimization) for this aspect.
- **The method body is skipped without a return value.** If an aspect skips the method body (`FlowBehavior.Return` in `OnEntry`, or `FlowBehavior.Continue`/`FlowBehavior.Return` in `OnException`) and no aspect sets `args.ReturnValue`, the method returns the default value of its return type. Version 2 threw a `NullReferenceException` for value types in this case.

The configuration in `FodyWeavers.xml` is unchanged; the optimization is enabled by default.

### Quickstart

1. Install the MethodBoundaryAspect.Fody NuGet package (`Install-Package MethodBoundaryAspect.Fody`)
2. Create `FodyWeavers.xml` in your project and add the Weaver `MethodBoundaryAspect` to it ([further details](https://github.com/Fody/Fody))
3. Write your custom aspects by deriving from `OnMethodBoundaryAspect` and decorate your methods (see sample below)

### Sample

Short sample how a transaction aspect could be implemented.

#### The aspect code

```csharp
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
```

#### The applied aspect

```csharp
public class Sample
{
    [TransactionScope]
    public void Method()
    {
        Debug.WriteLine("Do some database stuff isolated in surrounding transaction");
    }
}
```

### Additional Sample

Consider an aspect written like this.

#### The aspect code

```csharp
using static System.Console;
using MethodBoundaryAspect.Fody.Attributes;

public sealed class LogAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        WriteLine("On entry");
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        WriteLine("On exit");
    }

    public override void OnException(MethodExecutionArgs args)
    {
        WriteLine("On exception");
    }
}
```

#### The applied aspect

Suppose the aspect is applied to this method:

```csharp
using static System.Console;

public class Sample
{
    [Log]
    public void Method()
    {
        WriteLine("Entering original method");
        OtherClass.DoSomeOtherWork();
        WriteLine("Exiting original method");
    }
}
```

We would expect the method call to output this:

```
On entry
Entering original method
Exiting original method
On exit
```

If, however, the call to `OtherClass.DoSomeOtherWork()` throws an exception, then it will look like this instead:

```
On entry
Entering original method
On exception
```

Note that the `OnExit` handler is not called when an exception is thrown.

### Altering Method Behavior
#### Changing return values
In order to change the return value of a method, hook into its `OnExit` handler and set the `ReturnValue` property of the `MethodExecutionArgs`.

```csharp
using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Linq;
using static System.Console;

public sealed class IndentAttribute : OnMethodBoundaryAspect
{
    public override void OnExit(MethodExecutionArgs args)
    {
        args.ReturnValue = String.Concat(args.ReturnValue.ToString().Split('\n').Select(line => "  " + line));
    }
}

public class Program
{
    [Indent]
    public static string GetLogs() => @"Detailed Log 1
Detailed Log 2";

    public static void Main(string[] args)
    {
        WriteLine(GetLogs()); // Output: "  Detailed Log 1\n  Detailed Log 2";
    }
}
```  

This can also be used on async methods to add additional processing or exception-handling.

```csharp
using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Linq;
using static System.Console;

public sealed class HandleExceptionAttribute : OnMethodBoundaryAspect
{
    public override void OnExit(MethodExecutionArgs args)
    {
        if (args.ReturnValue is Task<string> task)
        {
          args.ReturnValue = task.ContinueWith(t =>
          {
            if (t.IsFaulted)
              return "An error happened: " + t.Exception.Message;
            return t.Result;
          });
        }
    }
}

public class Program
{
    [HandleException]
    public static async Task<string> Process()
    {
      await Task.Delay(10);
      throw new Exception("Bad data");
    }

    public static async Task Main(string[] args)
    {
        WriteLine(await Process()); // Output: "An error happened: Bad data"
    }
}
```  

#### Changing input arguments
In order to change the return value of a method, hook into its `OnEntry` handler and modify the elements of the `Arguments` property of the MethodExecutionArgs.  
Important:  
And you have to annotate your aspect with the `AllowChangingInputArgumentsAttribute` because the weaver has to generate additional code. For non-modifying aspects this code is unnecessary and would only cost performance.
No async support yet!

```csharp
using System;
using MethodBoundaryAspect.Fody.Attributes;

[AllowChangingInputArguments]
public sealed class InputArgumentIncrementorAttribute : OnMethodBoundaryAspect
{
    public int Increment { get; set; }

    public override void OnEntry(MethodExecutionArgs args)
    {
        var inputArguments = args.Arguments;
        for (var i = 0; i < inputArguments.Length; i++)
        {
            var value = inputArguments[i];
            if (value is int v)
                inputArguments[i] = v + Increment;
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        // ByValue
        MethodByValue(10);

        // ByRef
        var value = 10;
        MethodByRef(ref value);
        Console.WriteLine("after method call: " + value); // Output: 20
    }

    [InputArgumentIncrementor(Increment = 1)]
    public static void MethodByValue(int i)
    {
        Console.WriteLine(i); // Output: 11
    }

    [InputArgumentIncrementor(Increment = 10)]
    public static void MethodByRef(ref int i)
    {
        Console.WriteLine(i); // Output: 20
    }
}
```
  
### Asynchronous Sample

Consider the same aspect as above but now applied to this method:

```csharp
using static System.Console;

public class Sample
{
    [Log]
    public async Task MethodAsync()
    {
        WriteLine("Entering original method");
        await OtherClass.DoSomeOtherWorkAsync();
        WriteLine("Exiting original method");
    }
}
```

The `On entry` line will be written when `MethodAsync` is first called on the main thread.

The `Entering original method` line will be written shortly thereafter, on the main thread.

The `Exiting original method` line will be written only after the task returned by `OtherClass.DoSomeOtherWorkAsync` has completed. Depending on your context and whether the given task was already complete when it was returned, this may or may not be on the main thread.

The `On exit` line will be written when `MethodAsync` returns to its caller, which may be slightly after or long before the long-running task has completed, but it will occur synchronously on the main thread.

The `On exception` line will be written if `OtherClass.DoSomeOtherWorkAsync` throws an exception, returns null (thus causing a `NullReferenceException` when it is `await`ed), or returns a faulted task. As such, this may occur long after `MethodAsync` itself has returned its `Task` to its caller. The call to `OnException` will take place on whichever thread the synchronization context was running when the exception occurred.

Note that, unlike for synchronous methods, an aspect for an asynchronous method will have its `OnExit` handler called whether or not its `OnException` is called. Furthermore, unlike synchronous methods, the call to `OnException` may take place long after the call to `OnExit`. If this behavior is undesirable, consider using the `MethodExecutionTag` to track whether the `OnExit` has run before the `OnException`. One such solution looks like this.

```csharp
using static System.Console;
using MethodBoundaryAspect.Fody.Attributes;

public sealed class LogAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        WriteLine("On entry");
        arg.MethodExecutionTag = false;
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        WriteLine("On exit");
        arg.MethodExecutionTag = true;
    }

    public override void OnException(MethodExecutionArgs args)
    {
        if ((bool)arg.MethodExecutionTag)
          return;
        WriteLine("On exception");
    }
}
```

One additional note about the asynchronous behavior: the `OnExit` handler runs when the `MethodAsync` returns to its caller, not when the asynchronous code finishes running, which may be some time later. If you need code to be run when the method's asynchronous code finishes instead of when the actual method exits, consider a solution like the following:

```csharp
using static System.Console;
using MethodBoundaryAspect.Fody.Attributes;

public sealed class LogAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        WriteLine("On entry");
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        if (args.ReturnValue is Task t)
            t.ContinueWith(task => WriteLine("On exit"));
    }

    public override void OnException(MethodExecutionArgs args)
    {
        WriteLine("On exception");
    }
}
```

### Ref structs (`Span<T>`, `ReadOnlySpan<T>`, ...)

Ref structs cannot be boxed, so their values cannot be passed to the aspect via `MethodExecutionArgs`. Methods using them are still weaved, but:

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

A build warning is written for each weaved method using ref structs. To suppress these warnings, add the `SuppressRefStructWarnings` attribute to `FodyWeavers.xml`:

```xml
<Weavers>
  <MethodBoundaryAspect SuppressRefStructWarnings="true" />
</Weavers>
```

To exclude a single method from weaving instead, annotate it with `[DisableWeaving]`.

### Performance: only used `MethodExecutionArgs` properties are provided

Providing all `MethodExecutionArgs` properties on every call costs time and memory. The arguments are boxed into a new `object[]`, the return value is boxed, and the `MethodBase` is looked up (by reflection for open generic methods). When weaving, the IL of the aspect's `OnEntry`, `OnExit` and `OnException` methods is analyzed. Values that no aspect of a method uses are not provided:

| Not used by any aspect of the method | Not done at runtime |
|---|---|
| `args.Arguments` (read) | the arguments array is not created, the arguments are not boxed. Still done for `[AllowChangingInputArguments]` aspects |
| `args.ReturnValue` (read or write) | the return value is not boxed into `args.ReturnValue` before `OnExit` |
| `args.ReturnValue` (write) | the return value is not read back (unboxed) from `args.ReturnValue` after the aspect calls |
| `args.Method` | the `MethodBase` is not looked up |
| `args.Instance` | the instance is not set (and not boxed for structs) |

For example, only `args.Method` is provided for this aspect:

```csharp
public sealed class TimingAspect : OnMethodBoundaryAspect
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

The analysis is conservative. If `args` is used in any other way than reading or writing its properties (for example, passed to a logger, stored in a field, or captured by a lambda), the aspect gets all properties. Calls to non-virtual methods of the aspect and its base classes are followed, e.g. `base.OnEntry(args)` or a private helper. When several aspects are applied to a method, a property is provided if any of them uses it.

If the method body is skipped (`FlowBehavior.Return` in `OnEntry`, or `FlowBehavior.Continue`/`FlowBehavior.Return` in `OnException`) and no aspect sets `args.ReturnValue`, the method returns the default value of its return type. Before this optimization, a method returning a value type threw a `NullReferenceException` in this case (see [Breaking changes in version 3](#breaking-changes-in-version-3)).

#### Disabling the optimization

The analysis runs when the project using the aspect is weaved. If that project is not rebuilt when the aspect's assembly is updated (e.g. an aspect loaded from a plugin), a new aspect version could access properties that were not provided and are `null` (see [Breaking changes in version 3](#breaking-changes-in-version-3)). Aspects whose assembly can only be resolved as a reference assembly are never optimized.

To disable the optimization for single aspects, add their full type names to `FodyWeavers.xml` (nested types as `Namespace.Outer+Inner`):

```xml
<Weavers>
  <MethodBoundaryAspect>
    <DisableExecutionArgsOptimization Aspect="MyCompany.Logging.LogAspect" />
    <DisableExecutionArgsOptimization Aspect="MyCompany.Plugins.PluginAspect" />
  </MethodBoundaryAspect>
</Weavers>
```

A build warning is written if a configured aspect is not applied to any weaved method. To disable the optimization for all aspects:

```xml
<Weavers>
  <MethodBoundaryAspect DisableExecutionArgsOptimization="true" />
</Weavers>
```

## Benchmarks

* BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19043.1766/21H1/May2021Update)
* Intel Core i7-10700K CPU 3.80GHz, 1 CPU, 16 logical and 8 physical cores
* .NET SDK=7.0.100
    * [Host] : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
    * DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


|                       Method |     Mean |   Error |  StdDev |
|----------------------------- |---------:|--------:|--------:|
|            CallWithoutAspect | 108.8 ns | 0.50 ns | 0.44 ns |
|               CallWithAspect | 136.3 ns | 2.72 ns | 3.98 ns |
| OpenGenericCallWithoutAspect | 105.5 ns | 1.03 ns | 0.91 ns |
|    OpenGenericCallWithAspect | 201.0 ns | 3.37 ns | 2.82 ns |

