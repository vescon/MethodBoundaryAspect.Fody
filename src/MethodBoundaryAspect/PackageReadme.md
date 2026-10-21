# MethodBoundaryAspect.Fody

Run your own code when a method starts, ends or throws, by putting an attribute on it:

```csharp
[Log]
public int Add(int a, int b) => a + b;
```

MethodBoundaryAspect.Fody is a [Fody](https://github.com/Fody/Fody) weaver: the calls to your aspect are woven into the IL of the method **at build time**. There is no runtime proxy, no dependency injection container and no interface or virtual method required. It works for static, private, generic and async methods of any class, on .NET Framework 4.6.2+ and .NET (Core) / .NET Standard 2.0+.

Typical aspects: logging, measuring execution time, transaction handling, exception wrapping, showing a wait cursor, argument validation, caching.

## Quickstart

1. Add this package (it brings Fody along).

2. Add a `FodyWeavers.xml` file to the project root:

   ```xml
   <Weavers>
     <MethodBoundaryAspect />
   </Weavers>
   ```

3. Write an aspect by deriving from `OnMethodBoundaryAspect` and overriding any of `OnEntry`, `OnExit` and `OnException`:

   ```csharp
   using System;
   using MethodBoundaryAspect.Fody.Attributes;

   public sealed class LogAttribute : OnMethodBoundaryAspect
   {
       public override void OnEntry(MethodExecutionArgs args) =>
           Console.WriteLine($"Entering {args.Method.Name}({string.Join(", ", args.Arguments)})");

       public override void OnExit(MethodExecutionArgs args) =>
           Console.WriteLine($"{args.Method.Name} returned {args.ReturnValue}");

       public override void OnException(MethodExecutionArgs args) =>
           Console.WriteLine($"{args.Method.Name} failed: {args.Exception.Message}");
   }
   ```

4. Apply it to a method, a class, a property or the whole assembly (`[assembly: Log]`) and build:

   ```csharp
   public class Calculator
   {
       [Log]
       public int Add(int a, int b) => a + b;
   }

   new Calculator().Add(1, 2);
   // Entering Add(1, 2)
   // Add returned 3
   ```

## What an aspect can do

All three aspect methods get a `MethodExecutionArgs` with the information of the call:

| Property | Content |
|---|---|
| `Instance` | the object the method is called on, `null` for static methods |
| `Method` | the called method as `MethodBase` |
| `Arguments` | the argument values (can be changed with `[AllowChangingInputArguments]` on the aspect) |
| `ReturnValue` | the return value in `OnExit`, can be overwritten |
| `Exception` | the thrown exception in `OnException` |
| `FlowBehavior` | `Return` in `OnEntry` skips the method body, `Continue` in `OnException` swallows the exception |
| `MethodExecutionTag` | any object you want to pass from `OnEntry` to `OnExit`/`OnException` of the same call |

- **Select the methods** with `[DisableWeaving]`, regular expression filters for namespace, type and method name, and the visibility of the methods (`AttributeTargetMemberAttributes`).
- **Combine aspects** on one method and order them with `[AspectOrderIndex]`.
- **Async methods** are supported: `OnException` is called when the asynchronous work fails.
- **Low overhead:** the weaver only provides the `MethodExecutionArgs` properties your aspects use, and creates aspects without state once instead of for every call.

## Documentation

The complete documentation is the [README on GitHub](https://github.com/vescon/MethodBoundaryAspect.Fody#readme):

- [How it works](https://github.com/vescon/MethodBoundaryAspect.Fody#how-it-works), [Writing aspects](https://github.com/vescon/MethodBoundaryAspect.Fody#writing-aspects) and [Applying aspects](https://github.com/vescon/MethodBoundaryAspect.Fody#applying-aspects)
- [Changing the method behavior](https://github.com/vescon/MethodBoundaryAspect.Fody#changing-the-method-behavior) (return values, arguments, skipping the method, swallowing exceptions) and [Async methods](https://github.com/vescon/MethodBoundaryAspect.Fody#async-methods)
- [Limitations](https://github.com/vescon/MethodBoundaryAspect.Fody#limitations), [Performance](https://github.com/vescon/MethodBoundaryAspect.Fody#performance) and the [Configuration reference](https://github.com/vescon/MethodBoundaryAspect.Fody#configuration-reference)
- [Troubleshooting](https://github.com/vescon/MethodBoundaryAspect.Fody#troubleshooting) (aspect not called, fields reset, `null` properties)
- [Upgrading to version 3](https://github.com/vescon/MethodBoundaryAspect.Fody#upgrading-to-version-3) and the [Changelog](https://github.com/vescon/MethodBoundaryAspect.Fody/blob/master/CHANGELOG.md)
- Complete sample projects: [.NET](https://github.com/vescon/MethodBoundaryAspect.Fody/tree/master/Samples/HelloWorld_NetCore) and [.NET Framework](https://github.com/vescon/MethodBoundaryAspect.Fody/tree/master/Samples/HelloWorld_NetFramework)

Questions and bug reports: [GitHub issues](https://github.com/vescon/MethodBoundaryAspect.Fody/issues). MIT license.
