using System;
using MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects.Shared;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgram.NetFramework
{
    public class DebugInfoTest
    {
        [LogMethod]
        public void Execute(int number)
        {
            var square = number * number;
            Console.WriteLine($"{nameof(Execute)} - Square of '{number}' is '{square}'");
        }
    }
}