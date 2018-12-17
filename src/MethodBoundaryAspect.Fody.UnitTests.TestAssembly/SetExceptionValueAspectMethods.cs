using System;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class SetExceptionValueAspectMethods
    {
        public static object Result { get; set; }

        [SetExceptionValueAspect]
        public static void StaticMethodCall()
        {
            throw new InvalidOperationException("StaticMethodCall");
        }

        [SetExceptionValueAspect]
        public void InstanceMethodCall()
        {
            throw new InvalidOperationException("InstanceMethodCall");
        }
    }
}