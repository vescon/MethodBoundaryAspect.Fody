using System;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    [SetClassNameAspect]
    public class AnonymousMethods
    {
        public static object Result { get; set; }

        public void CallAnonymousMethod()
        {
            Action action = () => Result = "CallAnonymousMethod";
            action();
        }
    }
}