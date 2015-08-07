using System;
using System.IO;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class SpecialOperatorMethods
    {
        private static bool ReturnBool(string[] args, TryCatchMethods instance)
        {
            return true;
        }

        [SetMethodNameAspect]
        public object MethodWithTernaryOperatorAtTheEndAndTwoRetOpCodes(string[] args)
        {
            var instance = new TryCatchMethods();
            return ReturnBool(args, instance) ? null : instance;
        }

        public object MethodWithTernaryOperatorAtTheEndAndTwoRetOpCodesNoWeave(string[] args)
        {
            var instance = new TryCatchMethods();
            return ReturnBool(args, instance) ? null : instance;
        }

        [SetMethodNameAspect]
        public void MethodWithTryCatchAndUsing()
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    ms.WriteByte(0x42);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void MethodWithTryCatchAndUsingNoWeave()
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    ms.WriteByte(0x42);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public class ParameterWithNullableType
        {
            public Guid? NullableGuid { get; set; }
        }

        [SetMethodNameAspect]
        public string MethodWithParameterWithMultipleReturn(int i)
        {
            if (i == 1)
                return "NullableInt == null";


            return "";
        }

        public string MethodWithParameterWithMultipleReturnNoWeave(int i)
        {
            if (i == 1)
                return "NullableInt == null";


            return "";
        }

        public string MethodWithParameterWithNullableTypeNoWeaveNoTryCatch(int i)
        {
            if (i == 1)
                return "NullableInt == null";

            if (i == 2)
                throw new InvalidOperationException();

            if (i == 3)
                return "NullableInt != null";


            return "";
        }
    }
}