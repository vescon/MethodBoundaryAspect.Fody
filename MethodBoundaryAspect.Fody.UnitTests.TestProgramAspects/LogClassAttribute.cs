using System;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects
{
    public class LogClassAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            Console.WriteLine("LogClassAttribute->Method called: " + arg.Method.Name);
        }
    }
}