# MethodBoundaryAspect.Fody

[![Build status](https://ci.appveyor.com/api/projects/status/983caboro9uy91m9?svg=true)](https://ci.appveyor.com/project/marcells/methodboundaryaspect-fody)
[![NuGet](https://img.shields.io/nuget/v/MethodBoundaryAspect.Fody.svg)](https://www.nuget.org/packages/MethodBoundaryAspect.Fody/)

NuGet package https://www.nuget.org/packages/MethodBoundaryAspect.Fody/

## Introduction
Allows decorated method to access some runtime properties before and after method execution.
```csharp
 public class MethodExecutionArgs
  {
    public object Instance { get; set; }
    public MethodBase Method { get; set; }
    public object[] Arguments { get; set; }
    public object ReturnValue { get; set; }
    public Exception Exception { get; set; }
    public object MethodExecutionTag { get; set; }
  }
```
### Your Code

```csharp
	public sealed class TransactionScopeAttribute : OnMethodBoundaryAspect
    {
        public int TimeoutInSeconds { get; set; }

        public override void OnEntry(MethodExecutionArgs args)
        {
            var transactionScopeProvider = GlobalServiceLocator.ServiceLocator.GetInstance<ITransactionScopeProvider>();

            if (TimeoutInSeconds != 0)
                transactionScopeProvider.Timeout = TimeSpan.FromSeconds(TimeoutInSeconds);

            var transactionScope = transactionScopeProvider.Create();
            args.MethodExecutionTag = transactionScope;
        }

        public override void OnException(MethodExecutionArgs args)
        {
            FinishTransaction(args);
        }

        public override void OnExit(MethodExecutionArgs args)
        {
            FinishTransaction(args);
        }

        private static void FinishTransaction(MethodExecutionArgs args)
        {
            var transactionScope = (ITransactionScope)args.MethodExecutionTag;

            try
            {
                if (args.Exception == null)
                    transactionScope.Complete();
            }
            finally
            {
                if (transactionScope != null)
                    transactionScope.Dispose();
            }
        }
    }

	public class Sample
	{
		[TransactionScope]
		public void Method()
		{
		    Debug.WriteLine("Do some database stuff isolated in surrounding transaction");
		}
	}
```

License
-------

The MIT License (MIT)

Copyright (c) 2015 VESCON GmbH

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
