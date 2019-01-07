using System;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    [OnlyOnEntryAspect]
    public class AnonymousMethods
    {
        public object Result { get; set; }

        public void CallAnonymousMethod()
        {
            Action action = () => Result = "CallAnonymousMethod";
            action();
        }
    }
}