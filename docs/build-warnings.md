[Back to the README](../README.md)

# Build warnings and errors

The weaver writes a build warning instead of failing if it leaves something as it is or ignores a setting.

| Warning | Cause | What to do |
|---|---|---|
| `Method '...' is not woven: ...` | the method can't be woven: an async method of a task-like type with its own builder and an `OnException` aspect, a method with a variable argument list (`__arglist`), a method compiled with runtime-async (.NET 11, `<Features>runtime-async=on</Features>`, see [workarounds](async-methods.md#runtime-async-methods-net-11)), an async method whose state machine the compiler did not create in the expected way (the message names the reason) | [`[DisableWeaving]`](../README.md#excluding-methods-disableweaving) on the method removes the warning |
| `Method '...' uses ref struct values ...` | the method has `Span<T>` or other [ref structs](ref-structs.md) in its signature | `SuppressRefStructWarnings="true"` or `[DisableWeaving]` |
| `Aspect '...' is applied to the constructor/field/event/parameter ..., where it is ignored` | an aspect is applied where the weaver never reads it (the compiler accepts it, the aspect attribute has no `[AttributeUsage]`) | apply the aspect to the method, property, class or assembly |
| `Aspect '...' is applied to the interface/abstract/extern method (or property) ..., where it is ignored` | the method has no body to weave, and aspects are [not inherited](applying-aspects.md#several-aspects-on-one-method) by the methods which override or implement it | apply the aspect to the overrides and implementations |
| `Aspect '...' sets NamespaceFilter ...` | a filter is set in the constructor of the aspect or by overriding the property, the weaver ignores it | set it [where the aspect is applied](applying-aspects.md#filtering-by-name-and-visibility) |
| `Aspect '...' uses [ProvideAspectRole] or [AspectRoleDependency] ...` | the obsolete roles are used | use [`[AspectOrderIndex]`](applying-aspects.md#several-aspects-on-one-method) |
| `Aspect '...' configured in DisableExecutionArgsOptimization/DisableAspectInstanceReuse ... was not found` | the aspect named in `FodyWeavers.xml` is not applied to any woven method | use the full type name, or remove the entry |
| `Unknown setting '...' in FodyWeavers.xml is ignored` | a typo in the `MethodBoundaryAspect` element (e.g. `SupressRefStructWarnings`) | see the [Configuration reference](configuration.md) |

An invalid setup of an aspect (e.g. an invalid regular expression in a filter, two aspects of a method with the same order index) fails the build with a message that names the aspect or the method, e.g. `Aspect '...' has an invalid regular expression in ...` or `Method '...' could not be woven: ...`.
