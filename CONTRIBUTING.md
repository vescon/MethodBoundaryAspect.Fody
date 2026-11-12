# Contributing

Issues and pull requests are welcome. For a bigger change please open an issue first, so we can agree on the approach.

## Build and test

You need the .NET SDK 11 (the tests also run on .NET 8, 9 and 10 and on .NET Framework 4.6.2, 4.8 and 4.8.1, so the
.NET Framework 4.6.2 targeting pack must be installed on Windows).

```
dotnet build --configuration Release src/MethodBoundaryAspect.Fody.sln -m:1 -nodeReuse:false
dotnet test src/MethodBoundaryAspect.Fody.UnitTests.NetFramework --configuration Release --no-build
dotnet test src/MethodBoundaryAspect.Fody.UnitTests.NetCore --configuration Release --no-build
dotnet test src/MethodBoundaryAspect.Fody.RuntimeTests --configuration Release --no-build
```

- Build one project at a time (`-m:1`): a parallel build can fail with a locked `TestAssembly.NetFramework.dll`.
- `RuntimeTests` uses the weaver from `src/MethodBoundaryAspect.Fody/bin/<Configuration>`. After a change of the weaver build
  the solution (or the weaver project) first, then the test project.
- The NetFramework tests fail with "file name too long" when the repository is in a deep folder, use a short path.

## What a change needs

- **A test.** Behavior of woven code belongs into `RuntimeTests` (it runs on every runtime), details of the weaving (emitted
  IL, warnings, configuration) into `UnitTests.NetCore`.
- **Documentation.** A change users can notice goes into the [README](README.md) and into the section of the next version in
  the [CHANGELOG](CHANGELOG.md).
- **No new warnings.** The solution builds with `TreatWarningsAsErrors`.
- The weaver targets `netstandard2.0` only (Fody loads only that binary) and C# 8.

## How the weaver works

The section [How it works](README.md#how-it-works) of the README describes the idea, the classes of `src/MethodBoundaryAspect.Fody`
are documented in their summaries.
