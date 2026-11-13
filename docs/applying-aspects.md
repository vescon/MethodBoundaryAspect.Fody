[Back to the README](../README.md)

# Applying aspects: filters, configurations and order

The section [Applying aspects](../README.md#applying-aspects) of the README shows where an aspect can be applied. This page has the details of the filters, of aspects that are only woven in some build configurations and of several aspects on one method.

## Filtering by name and visibility

Aspects can be narrowed down with regular expressions (also an aspect applied directly to a method), which are matched against the namespace, the type name and the method name (property accessors are named `get_Name`/`set_Name`). Nested classes have the namespace of their outermost class, their type name is the name of the nested class itself (e.g. `Inner` for `Outer.Inner`). Generic types have their metadata name with the number of type parameters, e.g. ``Repository`1`` for `Repository<T>`:

```csharp
[assembly: Log(NamespaceFilter = @"^MyApp\.Services", TypeNameFilter = "Service$", MethodNameFilter = "^(?!get_|set_)")]
```

`AttributeTargetMemberAttributes` restricts the visibility of the methods (by default methods of any visibility are woven, `MulticastAttributes.AnyVisibility`). Methods of other visibilities are not rewritten at all. E.g. only public methods (and public property accessors):

```csharp
[assembly: Log(AttributeTargetMemberAttributes = MulticastAttributes.Public)]
```

or only public and internal methods:

```csharp
[assembly: Log(AttributeTargetMemberAttributes = MulticastAttributes.Public | MulticastAttributes.Internal)]
```

The filter is applied to the visibility of the method itself, so a public method of an internal class is public as well. The values map to the C# keywords like this:

| `MulticastAttributes` | C# |
|---|---|
| `Private` | `private` |
| `Protected` | `protected` |
| `Internal` | `internal` |
| `InternalAndProtected` | `private protected` |
| `InternalOrProtected` | `protected internal` |
| `Public` | `public` |

The filters see the names as they are in the compiled assembly. An explicit interface implementation (`void IDisposable.Dispose()`) is a `private` method named `System.IDisposable.Dispose`: `AttributeTargetMemberAttributes = MulticastAttributes.Public` excludes it, `^Dispose$` does not match it, and the example above, `^(?!get_|set_)`, does not exclude the accessors of an explicit implementation (`System.IFoo.get_Name`). Operators are named `op_Addition`, `op_Equality`, ..., and the accessors of an indexer `get_Item`/`set_Item`.

The regular expressions are not anchored and case sensitive: `"Service"` also matches `OrderServiceFactory`, use `^Service$` for the exact name. A filter that takes longer than 30 seconds to match fails the build.

`NamespaceFilter`, `TypeNameFilter`, `MethodNameFilter` and `AttributeTargetMemberAttributes` are read **where the aspect is applied** (as named arguments of the attribute). The weaver does not run the code of your aspects, so a value set in the constructor of the aspect, or by overriding the property, is ignored. A build warning is written for each aspect which does that.

## Applying an aspect only in some configurations

To weave an aspect only in some builds (e.g. a tracing aspect only in Debug), put `[Conditional("DEBUG")]` on the aspect class. The C# compiler leaves out every use of an attribute class with `[Conditional]` where the symbol is not defined, so the weaver never sees the aspect and the method is not changed:

```csharp
[Conditional("DEBUG")]
public class TracingAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args) { /* ... */ }
}

[Tracing] // in a Release build the compiler removes this attribute
public void DoWork() { }
```

The symbol has to be defined in the project where the aspect is applied (it works for assembly, class and method aspects, also if the aspect class is in another assembly). `#if DEBUG` around the attribute does the same for single places.

## Several aspects on one method

**Order.** Without further configuration `OnEntry` is called in this order, and in declaration order on the same level. `OnExit` and `OnException` are called in reverse order.

1. assembly
2. the enclosing classes, from the outermost to the innermost
3. the class
4. the property
5. the accessor or method

**Aspects are not inherited.** The weaver reads the attributes of the method itself:

- An aspect on a class applies to the methods declared in that class only, not to those of a derived class.
- An aspect on a virtual method does not apply to its overrides.
- An aspect on an interface (or on an interface method) does not apply to the classes that implement it. A build warning is written for an aspect on an abstract or interface method.
- The aspect attribute is inheritable, so reflection (`GetCustomAttributes`) reports it on an override, but that does not change what is woven.

Apply the aspect to the derived class or the override as well.

**The same aspect several times.**

- An aspect applied on several levels runs once per application: with `[Log]` on the class and on one of its methods, that method calls `Log` twice. Remove the attribute from the method to call it once.
- The same aspect can be applied twice to one member (e.g. with different arguments) only if the aspect class has `[AttributeUsage(AllowMultiple = true)]`; the compiler rejects it otherwise.

**Explicit order.** Use `[AspectOrderIndex]` on the assembly, class or method:

- The lower index runs `OnEntry` first.
- A method level index overrides a class level index, which overrides an assembly level index. For a nested class the index of the closest class that has one wins.
- If one aspect of a method has an index, all aspects of the method need an index.
- Two different aspects must not have the same index (the same aspect applied on several levels shares its index).

```csharp
[AspectOrderIndex(typeof(TransactionScopeAttribute), 1)]
[AspectOrderIndex(typeof(LogAttribute), 2)]
public class OrderService
{
    [Log, TransactionScope]
    public void PlaceOrder() { }  // TransactionScope.OnEntry, Log.OnEntry, body, Log.OnExit, TransactionScope.OnExit
}
```

`[ProvideAspectRole]`, `[AspectRoleDependency]` and `OnMethodBoundaryAspect.CompileTimeValidate` are obsolete and will be removed in version 4, see [Obsolete in version 3](upgrading-to-version-3.md#obsolete-in-version-3).
