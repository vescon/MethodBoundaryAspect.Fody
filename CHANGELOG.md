# Changelog

The changes of each release which can change the behavior of existing code (fixes of cases that did not work are
listed as well). The migration from version 2 is described in the
[README](README.md#upgrading-to-version-3). Versions are created from the Git tags, see the
[releases on GitHub](https://github.com/vescon/MethodBoundaryAspect.Fody/releases).

## Unreleased

- The runtime tests also run on .NET 11 (release candidate) and .NET Framework 4.8.1; no change for users.
- **`[AllowChangingInputArguments]` and `[AspectSkipProperties]` are inherited** by aspects derived from the aspect class that has the attribute. The weaver only read them from the aspect class itself, so a derived aspect silently could not change arguments or skip properties.
- **An invalid order of aspect roles fails the build with a message** naming the aspect (`InvalidAspectConfigurationException`), not with an empty `InvalidOperationException`.
- **A type that cannot be resolved** (missing in its assembly) no longer fails the weaver with a `NullReferenceException` where values of that type are passed to the aspect.
- `OnMethodBoundaryAspect.CompileTimeValidate` (obsolete) throws a `NotSupportedException` with an explanation instead of `NotImplementedException("TODO!")`.
- The XML documentation tells which `MethodExecutionArgs` values are `null`.

## 3.5.0

Mostly fixes of cases that did not work, listed because the behavior of existing code can change:

- **`[DisableWeaving]` on a property or an event is honored.** It was ignored there, a class or assembly aspect still hit the getter, setter, `add` and `remove` accessors.
- **A method that an aspect skips or whose exception it swallows returns a completed task**, not `null`, if it returns `Task` or `Task<T>` and no aspect sets `args.ReturnValue`.
- **A `ref` returning method throws a `NotSupportedException`** if its exception is swallowed (`FlowBehavior.Continue` in `OnException`), like it already did for `FlowBehavior.Return` in `OnEntry`. It returned a null reference before, which failed in the caller as soon as it used it.
- **Methods with a variable argument list (`__arglist`) are not woven** and a build warning is written. They were woven, but lost the variable arguments.
- **`[AllowChangingInputArguments]` works for async methods.** The changed arguments were ignored.
- **`null` for a value type argument passes the default value** instead of throwing a `NullReferenceException`.
- **Aspects without state are created once** instead of for every call, see [Aspect instances](README.md#aspect-instances). Opt-out: `DisableAspectInstanceReuse`.
- **The executor method keeps `[SkipLocalsInit]`, `MethodImplOptions.NoOptimization` and `AggressiveOptimization`** of the woven method.
- **`[AspectOrderIndex]` of an assembly and of an enclosing class** failed the build for the methods of a nested class. The closest level wins now.
- **Async methods whose aspects override none of the aspect methods** are left alone.
- Fixed: generic aspects (`[Aspect<int>]`) crashed at runtime, `[AspectOrderIndex]` on a nested class failed the build, a ref struct warning was written for methods the weaver does not change, and an invalid aspect setup was reported as an unhandled exception without the method.

## 3.4.0

Fixes of cases that did not work, listed because the behavior of existing code can change:

- **Init accessors, records and `in` parameters of virtual methods can be woven.** They crashed at runtime (`InvalidProgramException`, `AccessViolationException`).
- **A skipped method body or a swallowed exception returns the default value** if no aspect sets `args.ReturnValue`, instead of throwing a `NullReferenceException` for value types (also for async methods, where the swallowed exception could complete the task with an `InvalidCastException`).
- **`[AllowChangingInputArguments]`:** `in` arguments are not written back to the caller anymore, and `ref` arrays of enums (e.g. `ref Color[]`) are written back as references.
- **Methods with more than 255 arguments** passed the wrong arguments to the method from the 256th argument on.
- **The method infos of `args.Method` are loaded on the first call of each method**, not all at once on the first call of any woven method, and a method that can't be loaded doesn't break the others. The cache fields are numbered instead of named by a SHA256 hash, which failed in .NET Framework MSBuild and Visual Studio with the FIPS mode of Windows enabled.
- **An invalid regular expression in `NamespaceFilter`, `TypeNameFilter` or `MethodNameFilter`** fails the build with a message that names the aspect.
- The XML documentation of the attributes is part of the package, so IntelliSense shows it.
