using System;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects
{
    public class LogAssemblyAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            Console.WriteLine("LogAssemblyAttribute->Method called: " + arg.Method.Name);
        }
    }
}