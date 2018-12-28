using System.Diagnostics;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgram.NetFramework
{
    internal static class Program
    {
        private static void Main()
        {
            //If no debugger is attached launch the debugger
            if (Debugger.IsAttached == false)
                Debugger.Launch();
            Debugger.Break();

            var testClass = new TestClass();
            testClass.DoIt(7);
        }
    }
}