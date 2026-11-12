# Changelog

The changes of each release which can change the behavior of existing code (fixes of cases that did not work are
listed as well). The migration from version 2 is described in the
[README](README.md#upgrading-to-version-3). Versions are created from the Git tags, see the
[releases on GitHub](https://github.com/vescon/MethodBoundaryAspect.Fody/releases).

## 3.7.0

### Risk overview of the changes in 3.7.0

Which entries below can change existing code. The rating is an estimate: how likely a project is affected and how bad it is then.

| Risk | Change | Who is affected | What happens | What to do |
|---|---|---|---|---|
| Medium | Each aspect has its own `MethodExecutionTag` | Aspects which relied on the tag of the aspect before | The tag is `null` | Set the tag in the aspect, pass data in another way |
| Medium | New build warnings (unknown setting in `FodyWeavers.xml`, aspect applied where it is ignored incl. abstract, interface and `extern` methods, runtime-async method, "sets X in its code") | Projects with `TreatWarningsAsErrors` or `WarningsAsErrors` | The build fails | Fix the cause or suppress the warning |
| Medium | An invalid regular expression in a filter fails the build also after a filter that does not match; timeout of 30 seconds | Projects with a broken or very slow pattern | Build error with the aspect named (was ignored silently or hung) | Fix the pattern |
| Medium | Aspects are shared in more cases (extra private constructor, `[In] ref`); code for `MethodExecutionTag` and `FlowBehavior` only if used | Aspects with state the analysis does not see | A wrongly shared aspect would fail silently (the analysis is conservative on purpose) | `DisableAspectInstanceReuse`, `DisableExecutionArgsOptimization` |
| Low to medium | An aspect on the `get` or `set` accessor is woven | Code with `[Aspect] get` that never did anything | The aspect runs now | Remove the attribute or add `[DisableWeaving]` |
| Low | `FlowBehavior` is reset before `OnException` | Aspects that set `Continue` in `OnEntry` | The exception is rethrown unless `OnException` sets `Continue` or `Return` | Set it in `OnException` |
| Low | Finalizers are not woven | Class or assembly aspects hitting finalizers | The aspect no longer runs there | none, it was unsafe |
| Low | Runtime-async methods (.NET 11) are not woven | Projects using `runtime-async` | Method unwoven, warning | none |
| Low | `args.Arguments` in `OnExit` and `OnException` hold the `ref` and `out` values after the call | Aspects which expected the values from before the call | New values | Copy them in `OnEntry` |
| Low | A wrong argument type set by an aspect is not reported to `OnException` | Aspects which handled that error | The exception goes to the caller | none |
| Low | `ReturnValue` of `ref` returns, `ref` arguments (the variable of the caller, written back only if replaced), `[In] ref`, skipped body of a `ref` returning method calls `OnExit` of the aspects before | Aspects with `[AllowChangingInputArguments]` or on `ref` methods | Correct values, nothing lost on concurrent changes | none |
| Low | Async: tag of `OnExit` visible in `OnException`; the task is returned if the work fails while `OnExit` runs | Several aspects on async methods | Corrected edge cases | none |
| Low | An aspect with a `Synchronized` method is not shared; a generic aspect with a private type argument is created per call | Performance only; before: `MethodAccessException` | Works | none |
| Low | The executor method keeps `[DebuggerHidden]`, `[StackTraceHidden]` and similar attributes; debugger scopes | Debugging, stack traces, trimming | The executor behaves like the original | none |
| None | `1`/`0` as boolean settings, generic aspects (`[AspectOrderIndex]`, constructor parameter and property of the type parameter), the same aspect on class and method with an index, `TypedReference` on .NET Framework, schema and README in the package | Everyone | More inputs work, errors fixed | none |

### Changes

- **Faster calls of woven methods: the getter of a shared aspect is inlined, and the weaver emits the code for `MethodExecutionTag` and `FlowBehavior` only if an aspect uses it.** Measured on .NET 10: the overhead of two aspects on a method drops from 6.0 ns to 2.5 ns, of two aspects on an async method from 17 ns and 208 B to 3.5 ns and 144 B (the numbers of [docs/performance.md](docs/performance.md)). `args.Arguments` of a method without parameters is `Array.Empty<object>()` (no allocation, if the target framework has it). Aspects which are not analyzed (`DisableExecutionArgsOptimization`, the aspect passes `args` on) keep the full code.
- **The regular expressions of the aspect filters (`NamespaceFilter`, `TypeNameFilter`, `MethodNameFilter`) are cached and have a timeout of 30 seconds.** A pattern with catastrophic backtracking hung the build; it fails with a message naming the aspect and the filter now.
- **A build warning for a setting in `FodyWeavers.xml` the weaver does not know** (e.g. `SupressRefStructWarnings`). Typos were ignored silently.
- **`TypedReference`, `ArgIterator` and `RuntimeArgumentHandle` parameters work on .NET Framework.** The compiler does not mark them as ref structs there, so the weaver boxed them: the build failed in PEVerify or the method was an invalid program. They are passed as `null` like other ref structs.
- **A generic aspect with a private type as type argument** (`[Cached<Outer.PrivateKey>]`) **is created for every call instead of failing with a `MethodAccessException`.** The shared instance is created in a generated class, which cannot access the type.
- **An aspect with a `[MethodImpl(MethodImplOptions.Synchronized)]` method is no longer shared between the calls.** The shared instance was locked by every call, so the calls of the whole process waited for each other (and a deadlock was possible). The aspect is created for every call.
- **`args.Arguments` in `OnExit` contains the values of `ref` and `out` arguments after the call**, also if no aspect uses `[AllowChangingInputArguments]`. It contained the values from before the call.
- **An argument of the wrong type set by an aspect (`[AllowChangingInputArguments]`) is not reported to `OnException`.** `Arguments[0] = "x"` for an `int` threw the `InvalidCastException` inside the try block of the call, so `OnException` handled it (and could swallow it) as if the method had thrown. The arguments are converted before the call now, the exception is thrown to the caller without `OnException`.
- **The `ReturnValue` of a method with a `ref` return is handled safely.** An aspect which set the `ReturnValue` of a `ref readonly` method overwrote the read only variable it references (a `readonly` field). It is ignored now. Setting the `ReturnValue` of a `ref` method of a value type to `null` threw a `NullReferenceException`, the variable keeps its value now.
- **A `ref` argument changed by an aspect (`[AllowChangingInputArguments]`) is the variable of the caller, not a copy.** The weaver passed the address of a local copy to the method and wrote the value back after the call. A method with a `ref` return (`ref int Pick(ref int a) => ref a`) returned a reference into the discarded local variable of the woven method (memory corruption), and a value assigned to a `ref` argument before the method threw (and an aspect swallowed the exception) was lost.
- **An aspect with an additional private constructor is shared by the calls as well.** Only the constructor the attribute calls has to be accessible for the generated class.
- **The debugger scopes of a woven method are right.** The executor method (`$_executor_<Name>`) keeps the imports (namespaces for the watch window) and the constants of the original method, and its scope covers the closing brace, so the locals are visible there. The woven method gets a new scope instead of the old one with the variable names of the moved body.
- **`args.Arguments` in `OnException` contains the `ref` and `out` values the method wrote before it threw.** They were the values from before the call (`OnExit` after a swallowed exception saw them already, since the arguments are updated in the handler now).
- **A `ref` argument is only written back before the call if an aspect replaced it (`[AllowChangingInputArguments]`).** The value of the arguments array was written into the variable of the caller unconditionally, so a change by another thread while `OnEntry` ran was lost (`Interlocked.Add(ref shared, 1)`).
- **The code of a woven method keeps `[DebuggerHidden]`, `[DebuggerStepThrough]`, `[DebuggerNonUserCode]`, `[StackTraceHidden]`, `[HandleProcessCorruptedStateExceptions]`, `[SecurityCritical]`, `[UnconditionalSuppressMessage]`, `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`.** The attributes stayed on the wrapper only, so the debugger stepped into the hidden code, the trimming and AOT warnings of the code returned, and a `catch (AccessViolationException)` did not work on .NET Framework. The moved body (`$_executor_<Name>`) gets them as well.
- **The warning "Aspect ... sets X in its code" names a generic aspect correctly.** The suggestion was `[Int32>(NamespaceFilter = ...)]` for `[Validate<int>]`, it is `[Validate(NamespaceFilter = ...)]` now.
- **An invalid regular expression in a filter (`NamespaceFilter`, `TypeNameFilter`, `MethodNameFilter`) fails the build also if an earlier filter of the aspect does not match.** The filters were evaluated one after the other for each method and the first one that did not match stopped it, so the invalid expression was ignored silently.
- **A build warning for an aspect applied where it is ignored:** on a constructor, field, event, parameter, return value or the module (`[module: Log]`). The compiler accepts it there, the weaver never reads it, so nothing happened silently.
- **A build warning for an aspect on an abstract, interface or `extern` method or property.** It has no body to weave and aspects are not inherited, so the aspect never ran, also not for the overrides and implementations.
- **An aspect which is replaced by a newer version without rebuilding the project that uses it:** a `FlowBehavior` the new version sets is ignored if the old version did not set one (the code for it is only woven if used, see the first entry). Rebuild the project or use `DisableExecutionArgsOptimization`.
- **`1` and `0` are accepted as boolean values in `FodyWeavers.xml`** (`SuppressRefStructWarnings`, `DisableExecutionArgsOptimization`, `DisableAspectInstanceReuse`). The schema allows them, but the build failed with "Invalid value '1'".
- **A method compiled with runtime-async (.NET 11, `<Features>runtime-async=on</Features>`) is not woven, a build warning is written.** Such a method has no state machine and was woven like a synchronous method, which keeps the async flag at a method without an `await`. Weaving these methods is not supported yet.
- **Skipping the body of a method with a `ref` return calls the `OnExit` of the aspects before the aspect that skipped it,** like swallowing an exception of such a method already did. The method throws a `NotSupportedException` in both cases, but an aspect that opened a resource in `OnEntry` (a transaction) did not get its `OnExit` when the body was skipped.
- **Finalizers are not woven.** `[assembly: Log]` or an aspect on a class called the aspect on the finalizer thread, where an exception of the aspect ends the process, and `args.Instance` could resurrect the object.
- **A `ref` parameter with `[In]` (`void Inc([In] ref int x)`) is handled like a `ref` parameter.** The weaver treated it like an `in` parameter, so a value changed by an aspect (`[AllowChangingInputArguments]`) or by the method was not written back to the caller.
- **With several aspects on an async method, the `MethodExecutionTag` set in `OnExit` is seen in `OnException`** (the pattern of the README to find out if the method already returned its task). It was lost, because the tag was written to the state machine after it was started and copied; with one aspect it worked. The tags of async methods are kept in an array the copies share.
- **An async method returns its task even if the asynchronous work fails while `OnExit` runs.** The woven code read the `ReturnValue` back after `OnExit`, where the catch block of `MoveNext` (another thread) had replaced it by the result of the swallowed exception: the caller got a `NullReferenceException`/`InvalidCastException` or, for `ValueTask<T>`, a default value instead of its task. Rare, it needs a failure on another thread before the method returns.
- **A constructor parameter of the type parameter of a generic aspect** (`[Validate<int>(5)]`, constructor `Validate(T value)`) no longer fails the build with "Parametertype: T".
- **A property or field of the type parameter of a generic aspect can be set to a value type** (`[Validate<int>(Value = 5)]`). The woven code boxed the value before setting it, the method threw an `InvalidProgramException`.
- **The same aspect on the class and on the method can have an `[AspectOrderIndex]`.** The build failed with "No unique order index" because both applications have the same index; only different aspects need different indices.
- **`[AspectOrderIndex(typeof(MyAspect<int>), 1)]` works for generic aspects.** It was compared with the generic type definition and never applied, the build failed with "No order index provided". `typeof(MyAspect<>)` is still accepted.
- **`FlowBehavior` is reset before `OnException`:** `FlowBehavior.Continue` set in `OnEntry` (where it has no effect) made the method swallow a later exception if an aspect had an `OnException`, even if it did not ask for it. The exception is rethrown now unless `OnException` sets `Continue` or `Return`.
- **An aspect on the `get` or `set` accessor of a property** (`int X { [Log] get; }`) is woven into that accessor. It was ignored, only aspects on the property itself were read.
- **Each aspect has its own `MethodExecutionTag`:** with several aspects on a method, an aspect which did not set the tag saw the tag of the aspect before it in `OnEntry`, `OnExit` and `OnException`. It sees `null` now, as documented.
- **The configuration in `FodyWeavers.xml` has a schema** (`MethodBoundaryAspect.Fody.xcf`): IntelliSense and validation for `SuppressRefStructWarnings`, `DisableExecutionArgsOptimization` and `DisableAspectInstanceReuse`.
- The NuGet package contains a README (shown on nuget.org); no change for users of the library.
- **Uses of an attribute with the same values share one aspect instance.** `[Log]` on 1000 methods generated 1000 fields and getters in `OnMethodBoundaryAspectCompile.Aspects`, now one. A reusable aspect can not be told from a new instance, so this only makes the assembly smaller.
- **The executor method keeps the annotations of the generic parameters and the return value** (`[DynamicallyAccessedMembers]`, `[NotNull]`, ...). The parameters kept them already. The wrapper passes its type argument on to the executor, so the trimmer reported IL2091 in the woven code of an annotated generic method.
- The build messages say "woven" instead of "weaved" (`Method '...' is not woven`, `could not be woven`, `was not found in any woven method`). Only matters for a build log filter on the old wording.

## Planned for version 4

Announced in version 3 (obsolete attributes, warnings, README). Nothing of this is done yet. The migration will be described in the README under "Upgrading to version 4".

| Risk | Change | Notes |
|---|---|---|
| Medium | `[ProvideAspectRole]`, `[AspectRoleDependency]`, `AspectDependencyAction` and `AspectDependencyPosition` are removed | `[Obsolete]` and a build warning per aspect since 3.x; use `[AspectOrderIndex]` |
| Medium | Aspects applied directly to an event are woven into `add` and `remove` | No version did this so far; class and assembly aspects already hit the accessors; `[DisableWeaving]` on an event is honored since 3.5.0 |
| Low | `OnMethodBoundaryAspect.CompileTimeValidate` is removed | `[Obsolete]`, never called, throws `NotSupportedException` |
| Low | `NamespaceFilter`, `TypeNameFilter` and `MethodNameFilter` are no longer `virtual` | An override never had an effect; 3.x warns |
| Low | The usage of `[DisableWeaving]`, `[AspectOrderIndex]` and `[AspectSkipProperties]` is restricted to the targets the weaver reads | Compiler error instead of silently ignored; `[AllowChangingInputArguments]` is class only already |
| Medium | The minimum .NET Framework is raised from 4.6.2 to 4.7.2 (the package and the test projects) | Version 4 of `xunit.runner.visualstudio` and later supports net472 and higher only; Microsoft ends the support of 4.6.2 in January 2027. Fody is updated together with it |

## 3.6.0

The same code as 3.5.3, the version was named after the release of the changes listed under 3.5.2. No change for users.

## 3.5.3

- The runtime tests also run on .NET 11 (release candidate); no change for users.

## 3.5.2

- The runtime tests also run on .NET Framework 4.8.1; no change for users.
- **`[AllowChangingInputArguments]` and `[AspectSkipProperties]` are inherited** by aspects derived from the aspect class that has the attribute. The weaver only read them from the aspect class itself, so a derived aspect silently could not change arguments or skip properties.
- **An invalid order of aspect roles fails the build with a message** naming the aspect (`InvalidAspectConfigurationException`), not with an empty `InvalidOperationException`.
- **A type that cannot be resolved** (missing in its assembly) no longer fails the weaver with a `NullReferenceException` where values of that type are passed to the aspect.
- `OnMethodBoundaryAspect.CompileTimeValidate` (obsolete) throws a `NotSupportedException` with an explanation instead of `NotImplementedException("TODO!")`.
- The XML documentation tells which `MethodExecutionArgs` values are `null`.

## 3.5.1

- Publishing to nuget.org uses trusted publishing; no change for users.

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
