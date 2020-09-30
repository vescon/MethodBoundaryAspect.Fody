using System;
using MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects.Shared;

[assembly: ExceptionAssembly]

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgram.NetFramework
{
    [LogClass]
    public class LogTest
    {
        [LogMethod]
        public void DoIt(int number)
        {
            Console.WriteLine($"{nameof(DoIt)} - class - method body called with arg '{number}'");
        }
    }
}