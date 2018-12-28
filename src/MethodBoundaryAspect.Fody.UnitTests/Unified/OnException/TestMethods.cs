using System;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.Unified.OnException
{
    [OnlyOnExceptionAspect]
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
            if (i % 2 == 0)
            {
                Console.WriteLine("Even");
                return 0;
            }

            if (i % 2 != 0)
            {
                Console.WriteLine("Odd");
                return 1;
            }

            return 2;
        }

        public static ValueType2 SwitchCaseMethodWithReturnValueAndThrowAsDefaultValue(ValueType1 input)
        {
            switch (input)
            {
                case ValueType1.A: return ValueType2.A;
                case ValueType1.B: return ValueType2.B;
                case ValueType1.C: return ValueType2.C;
                default: throw new ArgumentOutOfRangeException(nameof(input), input, null);
            }
        }

        public static ValueType2 IfMethodWithReturnValueAndThrowAsDefaultValue(ValueType1 input)
        {
            if (input == ValueType1.A)
                return ValueType2.A;
            else if (input == ValueType1.B)
            {
                return ValueType2.B;
            }
            else if (input == ValueType1.C)
            {
                return ValueType2.C;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(input), input, null);
            }
        }

        public static void SwitchCaseMethodWithoutReturnValueAndThrowAsDefaultValue(ValueType1 input)
        {
            switch (input)
            {
                case ValueType1.A: return;
                case ValueType1.B: return;
                case ValueType1.C: return;
                default: throw new ArgumentOutOfRangeException(nameof(input), input, null);
            }
        }

        public static void IfMethodWithoutReturnValueAndThrowAsDefaultValue(ValueType1 input)
        {
            if (input == ValueType1.A)
                return;
            else if (input == ValueType1.B)
            {
                return;
            }
            else if (input == ValueType1.C)
            {
                return;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(input), input, null);
            }
        }
    }

    public enum ValueType1
    {
        A,
        B,
        C
    }

    public enum ValueType2
    {
        A,
        B,
        C
    }
}