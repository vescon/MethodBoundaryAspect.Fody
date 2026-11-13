[Back to the README](../README.md)

# Configuration reference

All settings are optional and go into the `MethodBoundaryAspect` element of `FodyWeavers.xml`:

```xml
<Weavers>
  <MethodBoundaryAspect SuppressRefStructWarnings="true">
    <DisableExecutionArgsOptimization Aspect="MyCompany.Logging.LogAttribute" />
    <DisableExecutionArgsOptimization Aspect="MyCompany.Plugins.PluginAttribute" />
    <DisableAspectInstanceReuse Aspect="MyCompany.Logging.CachingAttribute" />
  </MethodBoundaryAspect>
</Weavers>
```

| Setting | Effect |
|---|---|
| `SuppressRefStructWarnings="true"` | no build warnings for woven methods using [ref structs](ref-structs.md) |
| `<DisableExecutionArgsOptimization Aspect="..." />` | always provide all `MethodExecutionArgs` properties for this aspect (full type name, nested types as `Namespace.Outer+Inner`), see [Performance](performance.md). A build warning is written if the aspect is not applied to any woven method |
| `DisableExecutionArgsOptimization="true"` | the same for all aspects |
| `<DisableAspectInstanceReuse Aspect="..." />` | create this aspect for every call instead of sharing one instance, see [Aspect instances](performance.md#aspect-instances) (full type name, nested types as `Namespace.Outer+Inner`). A build warning is written if the aspect is not applied to any woven method |
| `DisableAspectInstanceReuse="true"` | the same for all aspects |

Errors in the configuration fail the build with a message: a value other than `true`, `false`, `1` or `0` for a boolean setting, and an element without the `Aspect` attribute. `Aspect` matches the exact type name only, a generic aspect is written with the number of its type parameters: ``Ns.Validate`1``.
