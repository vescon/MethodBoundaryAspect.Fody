[![Build status](https://img.shields.io/appveyor/ci/marcells/methodboundaryaspect-fody.svg)](https://ci.appveyor.com/project/marcells/methodboundaryaspect-fody)
[![Tests](https://img.shields.io/appveyor/tests/marcells/methodboundaryaspect-fody.svg)](https://ci.appveyor.com/project/marcells/methodboundaryaspect-fody/build/tests)
[![NuGet](https://img.shields.io/nuget/v/MethodBoundaryAspect.Fody.svg)](https://www.nuget.org/packages/MethodBoundaryAspect.Fody/)

## MethodBoundaryAspect.Fody
> A [Fody weaver](https://github.com/Fody/Fody) whichs injects custom code into the decorated method and provides information about the method parameters.

You can easily write your own aspects for
- transaction handling
- logging
- measuring method execution time
- exception wrapping
- displaying wait cursor
- and much more ...

### Supported features
- Hook into method start and end
- Hook into raised exceptions in a method
- Access method information like
    - applied object instance
    - the method itself ([System.Reflection.MethodBase](https://msdn.microsoft.com/en-us/library/system.reflection.methodbase))
    - the passed method arguments
    - the thrown exception
    - some custom object, which can be set at the start of the method and accessed at the end (e.g. useful for transactions or timers)
- Apply aspects at different levels
    - globally in `AssemblyInfo.cs`
    - on class
    - on method

Feel free to make a [Fork](https://github.com/vescon/MethodBoundaryAspect.Fody/fork) of this repository.

### Quickstart

1. Install the MethodBoundaryAspect.Fody NuGet package (`Install-Package MethodBoundaryAspect.Fody`)
2. Create `FodyWeavers.xml` in your project and add the Weaver `MethodBoundaryAspect` to it ([further details](https://github.com/Fody/Fody))
3. Write your custom aspects by deriving from `OnMethodBoundaryAspect` and decorate your methods (see sample below)

### Sample

Short sample how a transaction aspect could be implemented.

#### The aspect code

```csharp
public sealed class TransactionScopeAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = new TransactionScope();
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        var transactionScope = (TransactionScope)args.MethodExecutionTag;
        
        transactionScope.Complete();
        transactionScope.Dispose();
    }

    public override void OnException(MethodExecutionArgs args)
    {
        var transactionScope = (TransactionScope)args.MethodExecutionTag;
        
        transactionScope.Dispose();
    }
}
```

#### The applied aspect

```csharp
public class Sample
{
    [TransactionScope]
    public void Method()
    {
        Debug.WriteLine("Do some database stuff isolated in surrounding transaction");
    }
}
```