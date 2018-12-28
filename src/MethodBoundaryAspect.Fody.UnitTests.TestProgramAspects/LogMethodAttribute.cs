using System;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects.Shared
{
    public class LogMethodAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            Console.WriteLine("LogMethodAttribute->Method called: " + arg.Method.Name);
        }
    }
}