using System;
using System.Diagnostics;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgram.NetFramework
{
    internal static class Program
    {
        private static void Main()
        {
            //if no debugger is attached launch the debugger
            if (Debugger.IsAttached == false)
                Debugger.Launch();
            Debugger.Break();

            var t1 = new LogTest();
            t1.DoIt(7);

            var t2 = new InputArgumentsTest();
            t2.MethodWithByValue42(null);
            t2.MethodWithByValueException(null);

            var array = new[] { 1, 2, 3 };
            Console.WriteLine($"{nameof(InputArgumentsTest.MethodWithByRef)} - caller before:");
            InputArgumentsTest.DumpArray(array);
            t2.MethodWithByRef(ref array);
            Console.WriteLine($"{nameof(InputArgumentsTest.MethodWithByRef)} - caller after:");
            InputArgumentsTest.DumpArray(array);

            var array2 = new[] { 1, 2, 3 };
            Console.WriteLine($"{nameof(InputArgumentsTest.MethodWithByRefTwice)} - caller before:");
            InputArgumentsTest.DumpArray(array2);
            t2.MethodWithByRefTwice(ref array2);
            Console.WriteLine($"{nameof(InputArgumentsTest.MethodWithByRefTwice)} - caller after:");
            InputArgumentsTest.DumpArray(array2);

            var d = new DebugInfoTest();
            d.Execute(7);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}