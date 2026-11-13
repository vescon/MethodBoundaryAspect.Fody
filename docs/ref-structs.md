[Back to the README](../README.md)

# Ref structs (`Span<T>`, `ReadOnlySpan<T>`, ...)

Ref structs cannot be boxed, so their values cannot be passed to the aspect via `MethodExecutionArgs`. Methods using them are still woven, but:

- ref struct arguments (also `ref`, `in` and `out`) are `null` in `args.Arguments`
- a ref struct return value is `null` in `args.ReturnValue`
- the instance of a method declared in a `ref struct` is `null` in `args.Instance`
- changes to these values by the aspect are ignored (changed arguments, overwritten return value). If the aspect skips the method body (`FlowBehavior.Return`) or swallows an exception (`FlowBehavior.Continue`), the method returns the default value (e.g. an empty `Span<T>`)

```csharp
[Log]
public class Parser
{
    // args.Arguments is [null, 42] in OnEntry
    public bool TryParse(ReadOnlySpan<char> text, int maxLength) => text.Length <= maxLength;

    // args.ReturnValue is null in OnExit
    public Span<byte> Slice(byte[] buffer) => buffer.AsSpan(1);
}
```

A build warning is written for each woven method using ref structs. Suppress these warnings with `SuppressRefStructWarnings="true"` (see [Configuration reference](configuration.md)), or exclude the method from weaving with `[DisableWeaving]`.

Unmanaged pointers (`int*`, `void*`, function pointers) cannot be boxed either and are handled the same way, without a warning: they are `null` in `args.Arguments` and `args.ReturnValue` and passed unchanged to the method. `IntPtr` and `UIntPtr` are ordinary values and are provided.
