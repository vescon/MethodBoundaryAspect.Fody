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

The project targets `net8.0` and `net10.0`. The same benchmark on .NET 10 (default BenchmarkDotNet job, baseline 20.9 ns, the generic call without an aspect allocates nothing there):

| Aspect | Static method: time | Static method: allocated | Open generic method: time | Open generic method: allocated |
|---|---:|---:|---:|---:|
| uses no property | +2.8 ns | 72 B | +2.7 ns | 72 B |
| uses no property, created for every call (`DisableAspectInstanceReuse`) | +0.0 ns | 0 B | - | - |
| uses `args.Method` | +4.0 ns | 72 B | - | - |
| all properties provided (`DisableExecutionArgsOptimization`) | +11.3 ns | 152 B | +8.3 ns | 128 B |

The .NET 10 JIT removes the allocation of an aspect that is created for every call when the aspect does nothing and doesn't leak (escape analysis), so the numbers of that row only apply to such empty aspects.
