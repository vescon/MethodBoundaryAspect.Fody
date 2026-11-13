[Back to the README](../README.md)

# Performance

The aspect calls are woven into the IL at build time, so there is no reflection and no proxy at runtime. On top of that the weaver avoids work wherever it can prove that an aspect doesn't need it:

| Optimization | What it saves | Opt-out |
|---|---|---|
| [Only the `MethodExecutionArgs` properties that the aspects use are provided](#providing-only-what-is-used) | creating and boxing the arguments array, boxing the return value (and reading it back), setting the instance, looking up the method | `DisableExecutionArgsOptimization` |
| [Aspects without state are created once](#aspect-instances) | the allocation of the aspect on every call | `DisableAspectInstanceReuse` |
| [The method info is loaded once](#method-info-of-argsmethod) | the reflection call that resolves `args.Method`, on every call | - |
| Only the aspect methods you override are called | calls of empty base methods, and the `try`/`catch` for `OnException` if no aspect overrides it | - |
| The original body runs in a method marked `AggressiveInlining` | the JIT can inline it again, so there is usually no additional stack frame | - |
| `MethodExecutionTag` is only saved and restored with several aspects on a method, and only if one of them uses the tag | a local variable and two assignments per aspect | `DisableExecutionArgsOptimization` |
| `FlowBehavior` is only checked if an aspect of the method sets it | the checks after `OnEntry` and `OnException` | `DisableExecutionArgsOptimization` |
| Async methods get no wrapper method | no additional call level, the aspect state lives in the compiler-generated state machine | - |

What an aspect costs with and without these optimizations: see [Measured overhead](#measured-overhead).

## Providing only what is used

Filling all `MethodExecutionArgs` properties on every call costs time and memory: the arguments are boxed into a new `object[]`, the return value is boxed, and the `MethodBase` is loaded. Therefore the weaver analyzes the IL of the aspects' `OnEntry`, `OnExit` and `OnException` methods, and values that no aspect of a method uses are not provided:

| Not used by any aspect of the method | Not done at runtime |
|---|---|
| `args.Arguments` (read) | the arguments array is not created, the arguments are not boxed. Still done for `[AllowChangingInputArguments]` aspects |
| `args.ReturnValue` (read or write) | the return value is not boxed into `args.ReturnValue` before `OnExit` |
| `args.ReturnValue` (write) | the return value is not read back (unboxed) from `args.ReturnValue` after the aspect calls |
| `args.Method` | the `MethodBase` is not looked up |
| `args.Instance` | the instance is not set (and not boxed for structs) |
| `args.MethodExecutionTag` | with several aspects: the tag is not saved and restored for each aspect |
| `args.FlowBehavior` (write) | the `FlowBehavior` is not checked after `OnEntry` and `OnException`: the body always runs, an exception is always rethrown |

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

The analysis runs when the project using the aspect is woven. If that project is not rebuilt when the aspect's assembly is updated (e.g. an aspect loaded from a plugin), a new aspect version could access properties that were not provided and are `null`, a `FlowBehavior` it sets is ignored, and with several aspects it sees the `MethodExecutionTag` of another aspect. Disable the optimization for such aspects, see [Configuration reference](configuration.md).

## Method info of `args.Method`

`args.Method` is not resolved by reflection on every call. The weaver generates the internal static class `OnMethodBoundaryAspectCompile.MethodInfos` with a field and a getter for each woven method. The getter loads the `MethodBase` the first time it is called (from the metadata token of the method, with the declaring type for methods of generic types and for generic methods) and returns it from the field afterwards. Each method has its own getter, so a method whose `MethodBase` cannot be loaded does not affect the others. For generic methods and methods of generic types `args.Method` is the open definition (`typeof(T)` is not substituted), the type arguments of the call are not part of it.

## Aspect instances

An aspect that holds no state is created once and shared by all calls, instead of being created for every call. The weaver decides this by analyzing the IL of the aspect, and is conservative: an aspect is only shared if it can't be told from a new instance, i.e. all of these hold for the aspect and its base aspects:

- the instance fields are primitives, enums, `string` or `Type` (no collections, arrays or other objects), and only the constructor writes them
- the constructors only store their arguments and constants in fields (no counting of instances, no reading of a configuration, ...), and the property setters only store the value
- no method of the aspect calls a setter, takes the address of a field, captures `this` in a lambda or hands `this` to other code (calling its own methods and `GetType()` is fine)
- no finalizer, no `extern` method and no `[MethodImpl(MethodImplOptions.Synchronized)]` method (a shared instance would be locked by all calls), and the aspect and its constructors are accessible from the generated code (a `private` nested aspect, a non-public aspect of another assembly, or a generic aspect with a private type argument is created for every call)
- the aspect is no open generic type. Uses of an attribute with the same constructor, arguments, properties and fields share one instance: `[Log]` on 1000 methods is one instance, `[Log(Level = 1)]` and `[Log(Level = 2)]` are two

Other aspects behave as before, a new instance for every call. Use `args.MethodExecutionTag` for data of a call, as the instance is shared between concurrent calls.

The weaver decides when the project is woven, like for the [`MethodExecutionArgs` optimization](#providing-only-what-is-used). If the assembly of the aspect is replaced by a version holding state and the project using it is not rebuilt, the new version is still shared. Rebuild the projects using the aspect after changing it. Sharing is disabled in the [configuration](configuration.md) for all aspects, or for one aspect if it causes problems.

## Tips for hot paths

- Use only the `args` properties you need, and don't pass `args` to other code (see above). Providing `Arguments` and `ReturnValue` costs the most: an array and boxing.
- Keep aspects free of state: use `args.MethodExecutionTag` for the data of a call. Then the aspect is [shared](#aspect-instances).
- Override only the aspect methods you need. An aspect with `OnEntry` only has no `try`/`catch` and no `OnExit` call.
- `[AllowChangingInputArguments]` makes the weaver create the arguments array and copy the values back, use it only for aspects that change arguments.
- Weave less: [filter](applying-aspects.md#filtering-by-name-and-visibility) class and assembly aspects by namespace, type, method name or visibility, and exclude hot paths with [`[DisableWeaving]`](../README.md#excluding-methods-disableweaving).

## Measured overhead

An aspect with `OnEntry` and `OnExit` that uses no `args` property costs about **3 ns and 72 bytes** per call (a static method, .NET 8 and 10, BenchmarkDotNet [benchmark](../src/MethodBoundaryAspect.Fody.Benchmark/Program.cs)), providing all `args` properties costs about 11 ns and 152 bytes.

Overhead of an aspect with `OnEntry` and `OnExit` compared to the same call without an aspect ([benchmark](../src/MethodBoundaryAspect.Fody.Benchmark/Program.cs), .NET 8, BenchmarkDotNet short run). The allocations are what the call allocates in total (the call without an aspect allocates nothing, the generic one 24 B); the times of a short run vary by some ns between machines and runs:

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

The project targets `net8.0` and `net10.0` (use `-f net8.0` for the first table, `-f net10.0` for the tables below). The same benchmark on .NET 10 (default BenchmarkDotNet job, a longer and more exact run than the short job above, so the two tables are not comparable with each other; baseline 20.9 ns, the generic call without an aspect allocates nothing there):

| Aspect | Static method: time | Static method: allocated | Open generic method: time | Open generic method: allocated |
|---|---:|---:|---:|---:|
| uses no property | +1.9 ns | 72 B | +1.9 ns | 72 B |
| uses no property, created for every call (`DisableAspectInstanceReuse`) | +0.0 ns | 0 B | - | - |
| uses `args.Method` | +3.1 ns | 72 B | - | - |
| all properties provided (`DisableExecutionArgsOptimization`) | +10.2 ns | 152 B | +7.5 ns | 128 B |

Several aspects and async methods on .NET 10 (the call without an aspect: 20.9 ns, the async call without an aspect: 26.9 ns). The weaver only emits the code for `MethodExecutionTag` and `FlowBehavior` if an aspect uses them:

| Case | Time | Allocated |
|---|---:|---:|
| two aspects without properties | +2.5 ns | 72 B |
| method without parameters, `args.Arguments` read | +3.9 ns | 72 B |
| async method, one aspect | +2.2 ns | 144 B |
| async method, two aspects | +3.5 ns | 144 B |

The .NET 10 JIT removes all allocations of a call with an aspect that is created for every call when the aspect does nothing and doesn't leak (escape analysis: the aspect, the `MethodExecutionArgs` and the arguments array stay on the stack), which is why that row is faster than the shared aspect, 0 B instead of 72 B. The numbers of that row only apply to such empty aspects.
