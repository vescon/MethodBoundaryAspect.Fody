using System;
using System.Collections.Generic;
using System.Linq;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    [FirstAspect]
    public class SpecialMethodBodies
    {
        public static void MethodBodyEndsWithBrtrueOpcode()
        {
            var x = new List<string>();
            if (x.Any())
            {
                Console.WriteLine("test");
            }
        }

        public static void MethodBodyWithBrtrueOpcodeEndsWithBrtrueOpcode()
        {
            var x = new List<string>();
            if (!x.Any())
            {
                Console.WriteLine("test");
            }

            if (x.Any())
            {
                Console.WriteLine("test");
            }
        }

        public static void MethodBodyEndsWithBleOpcode()
        {
            var x = 5;
            if (x < 3)
            {
                Console.WriteLine("test");
            }
        }
    }
}