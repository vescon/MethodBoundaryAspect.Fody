using System;
using MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgram
{
    [LogClass]
    public class TestClass
    {
        [LogMethod]
        public void DoIt(int zahl)
        {
            Console.WriteLine("<method body called with arg '{0}'>", zahl);
        }
    }
}