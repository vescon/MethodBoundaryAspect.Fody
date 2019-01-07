using System;
using MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects;
using MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects.Shared;

[assembly: ExceptionAssembly]

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgram.NetFramework
{
    [LogClass]
    public class TestClass
    {
        public delegate void DoNothing();

        [LogMethod]
        public void DoIt(int zahl)
        {
            Console.WriteLine("<method body called with arg '{0}'>", zahl);
        }
    }
}