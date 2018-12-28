using System;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.Unified.OnEntry
{
    [OnlyOnEntryAspect]
    public class TestMethods
    {
        public static void VoidEmptyMethod()
        {
        }

        public static int IntMethod()
        {
            return 0;
        }

        public static int IntMethodInt(int i)
        {
            return i;
        }

        public static void VoidThrowMethod()
        {
            throw new Exception();
        }

        public static int IntMethodIntWithMultipleReturn(int i)
        {
            if (i%2 == 0)
            {
                Console.WriteLine("Even");
                return 0;
            }

            if (i%2 != 0)
            {
                Console.WriteLine("Odd");
                return 1;
            }

            return 2;
        }

        public void VoidThrowMethodTryCatch()
        {
            try
            {
                throw new InvalidOperationException("VoidThrowMethodTryCatch");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}