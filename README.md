[![Build status](https://img.shields.io/appveyor/ci/marcells/methodboundaryaspect-fody.svg)](https://ci.appveyor.com/project/marcells/methodboundaryaspect-fody)
[![Tests](https://img.shields.io/appveyor/tests/marcells/methodboundaryaspect-fody.svg)](https://ci.appveyor.com/project/marcells/methodboundaryaspect-fody/build/tests)
[![NuGet](https://img.shields.io/nuget/v/MethodBoundaryAspect.Fody.svg)](https://www.nuget.org/packages/MethodBoundaryAspect.Fody/)

## MethodBoundaryAspect.Fody
> A [Fody weaver](https://github.com/Fody/Fody) which allows to decorate methods and hook into method start, method end and method exceptions. Additionally you have access to useful method parameters.

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

### Additional Sample

Consider an aspect written like this.

#### The aspect code

```csharp
using static System.Console;

public sealed class LogAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        WriteLine("On entry");
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        WriteLine("On exit");
    }

    public override void OnException(MethodExecutionArgs args)
    {
        WriteLine("On exception");
    }
}
```

#### The applied aspect

Suppose the aspect is applied to this method:

```csharp
using static System.Console;

public class Sample
{
    [Log]
    public void Method()
    {
        WriteLine("Entering original method");
        OtherClass.DoSomeOtherWork();
        WriteLine("Exiting original method");
    }
}
```

We would expect the method call to output this:

```
On entry
Entering original method
Exiting original method
On exit
```

If, however, the call to `OtherClass.DoSomeOtherWork()` throws an exception, then it will look like this instead:

```
On entry
Entering original method
On exception
```

Note that the `OnExit` handler is not called when an exception is thrown.

### Asynchronous Sample

Consider the same aspect as above but now applied to this method:

```csharp
using static System.Console;

public class Sample
{
    [Log]
    public async Task MethodAsync()
    {
        WriteLine("Entering original method");
        await OtherClass.DoSomeOtherWorkAsync();
        WriteLine("Exiting original method");
    }
}
```

The `On entry` line will be written when `MethodAsync` is first called on the main thread.

The `Entering original method` line will be written shortly thereafter, on the main thread.

The `Exiting original method` line will be written only after the task returned by `OtherClass.DoSomeOtherWorkAsync` has completed. Depending on your context and whether the given task was already complete when it was returned, this may or may not be on the main thread.

The `On exit` line will be written when `MethodAsync` returns to its caller, which may be slightly after or long before the long-running task has completed, but it will occur synchronously on the main thread.

The `On exception` line will be written if `OtherClass.DoSomeOtherWorkAsync` throws an exception, returns null (thus causing a `NullReferenceException` when it is `await`ed), or returns a faulted task. As such, this may occur long after `MethodAsync` itself has returned its `Task` to its caller. The call to `OnException` will take place on whichever thread the synchronization context was running when the exception occurred.

Note that, unlike for synchronous methods, an aspect for an asynchronous method will have its `OnExit` handler called whether or not its `OnException` is called. Furthermore, unlike synchronous methods, the call to `OnException` may take place long after the call to `OnExit`. If this behavior is undesirable, consider using the `MethodExecutionTag` to track whether the `OnExit` has run before the `OnException`. One such solution looks like this.

```csharp
using static System.Console;

public sealed class LogAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        WriteLine("On entry");
        arg.MethodExecutionTag = false;
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        WriteLine("On exit");
        arg.MethodExecutionTag = true;
    }

    public override void OnException(MethodExecutionArgs args)
    {
        if ((bool)arg.MethodExecutionTag)
          return;
        WriteLine("On exception");
    }
}
```

One additional note about the asynchronous behavior: the `OnExit` handler runs when the `MethodAsync` returns to its caller, not when the asynchronous code finishes running, which may be some time later. If you need code to be run when the method's asynchronous code finishes instead of when the actual method exits, consider a solution like the following:

```csharp
using static System.Console;

public sealed class LogAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        WriteLine("On entry");
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        if (args.ReturnValue is Task t)
            t.ContinueWith(task => WriteLine("On exit"));
    }

    public override void OnException(MethodExecutionArgs args)
    {
        WriteLine("On exception");
    }
}
```
