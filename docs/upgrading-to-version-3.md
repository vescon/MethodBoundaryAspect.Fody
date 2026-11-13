[Back to the README](../README.md)

# Upgrading to version 3

Version 3 only provides the `MethodExecutionArgs` properties that the aspects of a method use (see [Performance](performance.md)). Which properties are used is determined when the project using the aspect is woven. This changes the runtime behavior in these cases:

- **The aspect code changes without the woven project being woven again.** If the aspect's assembly is replaced by a newer version (e.g. a plugin, a separately deployed library, or a binding redirect) and the project using the aspect is not rebuilt, the new aspect version gets `null` for the properties the old version didn't use (`Arguments`, `Method`, `Instance`, `ReturnValue`), and a `ReturnValue` or (since 3.7.0) a `FlowBehavior` it sets is ignored. Rebuild all projects using the aspect after changing it, or [disable the optimization](configuration.md) for this aspect.
- **The method body is skipped without a return value.** If an aspect skips the method body (`FlowBehavior.Return` in `OnEntry`, or `FlowBehavior.Continue`/`FlowBehavior.Return` in `OnException`) and no aspect sets `args.ReturnValue`, the method returns the default value of its return type. Version 2 threw a `NullReferenceException` for value types in this case.

Also changed:

- **`MulticastAttributes.AnyVisibility` weaves methods of all visibilities** ([#132](https://github.com/vescon/MethodBoundaryAspect.Fody/issues/132)). In version 2 it had the same value as `MulticastAttributes.Public`, so aspects with `AttributeTargetMemberAttributes = MulticastAttributes.AnyVisibility` only hit public methods. Now private, protected and internal methods are woven as well, like for aspects without `AttributeTargetMemberAttributes`. Use `MulticastAttributes.Public` to keep the previous behavior. An explicit `MulticastAttributes.Default` also weaves all visibilities now (version 2: no methods at all). The default value of the `AttributeTargetMemberAttributes` property (as read in aspect code at runtime) is `MulticastAttributes.AnyVisibility`, i.e. 126; in version 2 it was 64, the value of `Public`.
- **An exception thrown in `OnException` of an async method faults the returned task** ([#5](https://github.com/vescon/MethodBoundaryAspect.Fody/issues/5)). In version 2 it escaped the compiler-generated state machine: it was thrown synchronously to the caller if the method had not awaited anything yet (and the task never completed), otherwise it was unhandled on the thread pool and terminated the process.
- **Aspect classes are never woven** ([#70](https://github.com/vescon/MethodBoundaryAspect.Fody/issues/70)). Version 2 only skipped weaving an aspect into its own class. An assembly or class aspect was still woven into the other aspects of the assembly, into base aspect classes and into classes nested in an aspect. That caused a `StackOverflowException` when two aspects were woven into each other, or when the aspect's `OnEntry` was inherited from a base aspect in the same assembly. Now no aspect is woven into a class derived from `OnMethodBoundaryAspect` or its nested classes, also if it is applied directly to one of their methods.
- **Class and assembly aspects no longer hit the state machines of async and iterator methods** ([#66](https://github.com/vescon/MethodBoundaryAspect.Fody/issues/66)). In version 2 they were also woven into the compiler-generated `MoveNext` and `SetStateMachine` methods (and the other members of iterator state machines), so the aspect was additionally called with `args.Method.Name == "MoveNext"` each time an async method started or resumed after an `await`, and each time an iterator was advanced. The async or iterator method itself is still woven.

Changed in the releases of version 3 since 3.0 (all of them with the reason are in the [CHANGELOG](../CHANGELOG.md), the section 3.7.0 has a table with a risk rating):

- **Aspects without state are created once** and shared by all calls (3.5.0). Opt out with `DisableAspectInstanceReuse` (see [Aspect instances](performance.md#aspect-instances)).
- **Each aspect has its own `MethodExecutionTag`.** An aspect which did not set it sees `null`, not the tag of the aspect before it.
- **An aspect on the `get` or `set` accessor** (`int X { [Log] get; }`) is woven. It was silently ignored in version 2 and 3.0 to 3.6.
- **`[DisableWeaving]` on a property or event** is honored (3.5.0); finalizers and methods with `__arglist` are not woven.
- **`FlowBehavior` is reset before `OnException`.** `Continue` set in `OnEntry` no longer makes the method swallow a later exception.
- **`args.Arguments` in `OnExit` and `OnException`** hold the values of `ref` and `out` arguments after the call.
- **New build warnings and errors** which can fail a build with `TreatWarningsAsErrors`: unknown settings in `FodyWeavers.xml`, an aspect applied where it is ignored (constructor, field, event, parameter, return value, module, an abstract, interface or `extern` method), a method compiled with runtime-async, an invalid or very slow filter regular expression (error). See [Build warnings and errors](build-warnings.md).
- **Skipped or swallowed methods** return a completed task (`Task`, `Task<T>`) and the default value instead of `null`; a `ref` returning method throws a `NotSupportedException` then.

Planned for version 4: the aspect roles and `CompileTimeValidate` are removed, aspects applied directly to an event are woven, and the `virtual` filter properties and the usage of some attributes change (list with risk rating: [CHANGELOG](../CHANGELOG.md#planned-for-version-4)). Aspects directly on an event are not woven in any version so far.

The configuration in `FodyWeavers.xml` is unchanged; the optimization is enabled by default.

All changes of later releases are listed in the [CHANGELOG](../CHANGELOG.md).

## Obsolete in version 3

`[ProvideAspectRole]` and `[AspectRoleDependency]` are obsolete and will be removed in version 4, use [`[AspectOrderIndex]`](applying-aspects.md#several-aspects-on-one-method) instead. A build warning is written for each aspect using them. If one aspect of a method uses a role, an `[AspectOrderIndex]` on any aspect of that method fails the build (the two ways of ordering exclude each other; the message doesn't name the aspect), and `AspectDependencyAction.None` still orders. They have known problems which are not fixed: a role is ignored if its aspect has no `[AspectRoleDependency]` itself, and a chain of dependencies (A after B, B after C) is rejected with an `InvalidAspectConfigurationException`. `OnMethodBoundaryAspect.CompileTimeValidate` is obsolete as well: the weaver never calls it (it does not run the code of aspects), and it will be removed in version 4.
