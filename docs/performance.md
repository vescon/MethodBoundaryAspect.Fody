# Measured overhead

How much an aspect costs at runtime: this page belongs to the
[Performance section of the README](../README.md#performance), which describes what the weaver does to keep it low.

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
