# Changelog

The changes of each release which can change the behavior of existing code (fixes of cases that did not work are
listed as well). The migration from version 2 is described in the
[README](README.md#upgrading-to-version-3). Versions are created from the Git tags, see the
[releases on GitHub](https://github.com/vescon/MethodBoundaryAspect.Fody/releases).

## Unreleased

- **An argument of the wrong type set by an aspect (`[AllowChangingInputArguments]`) is not reported to `OnException`.** `Arguments[0] = "x"` for an `int` threw the `InvalidCastException` inside the try block of the call, so `OnException` handled it (and could swallow it) as if the method had thrown. The arguments are converted before the call now, the exception is thrown to the caller without `OnException`.
- **The `ReturnValue` of a method with a `ref` return is handled safely.** An aspect which set the `ReturnValue` of a `ref readonly` method overwrote the read only variable it references (a `readonly` field). It is ignored now. Setting the `ReturnValue` of a `ref` method of a value type to `null` threw a `NullReferenceException`, the variable keeps its value now.
- **A `ref` argument changed by an aspect (`[AllowChangingInputArguments]`) is the variable of the caller, not a copy.** The weaver passed the address of a local copy to the method and wrote the value back after the call. A method with a `ref` return (`ref int Pick(ref int a) => ref a`) returned a reference into the discarded local variable of the woven method (memory corruption), and a value assigned to a `ref` argument before the method threw (and an aspect swallowed the exception) was lost.
- **With several aspects on an async method, the `MethodExecutionTag` set in `OnExit` is seen in `OnException`** (the pattern of the README to find out if the method already returned its task). It was lost, because the tag was written to the state machine after it was started and copied; with one aspect it worked. The tags of async methods are kept in an array the copies share.
- **An async method returns its task even if the asynchronous work fails while `OnExit` runs.** The woven code read the `ReturnValue` back after `OnExit`, where the catch block of `MoveNext` (another thread) had replaced it by the result of the swallowed exception: the caller got a `NullReferenceException`/`InvalidCastException` or, for `ValueTask<T>`, a default value instead of its task. Rare, it needs a failure on another thread before the method returns.
- **A constructor parameter of the type parameter of a generic aspect** (`[Validate<int>(5)]`, constructor `Validate(T value)`) no longer fails the build with "Parametertype: T".
- **A property or field of the type parameter of a generic aspect can be set to a value type** (`[Validate<int>(Value = 5)]`). The woven code boxed the value before setting it, the method threw an `InvalidProgramException`.
- **The same aspect on the class and on the method can have an `[AspectOrderIndex]`.** The build failed with "No unique order index" because both applications have the same index; only different aspects need different indices.
- **`[AspectOrderIndex(typeof(MyAspect<int>), 1)]` works for generic aspects.** It was compared with the generic type definition and never applied, the build failed with "No order index provided". `typeof(MyAspect<>)` is still accepted.
- **`FlowBehavior` is reset before `OnException`:** `FlowBehavior.Continue` set in `OnEntry` (where it has no effect) made the method swallow a later exception if an aspect had an `OnException`, even if it did not ask for it. The exception is rethrown now unless `OnException` sets `Continue` or `Return`.
- **An aspect on the `get` or `set` accessor of a property** (`int X { [Log] get; }`) is woven into that accessor. It was ignored, only aspects on the property itself were read.
- **Each aspect has its own `MethodExecutionTag`:** with several aspects on a method, an aspect which did not set the tag saw the tag of the aspect before it in `OnEntry`, `OnExit` and `OnException`. It sees `null` now, as documented.
- The NuGet package contains a README (shown on nuget.org); no change for users of the library.

## 3.6.0

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
