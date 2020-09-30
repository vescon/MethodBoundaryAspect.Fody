using System;
using System.Collections.Generic;
using MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects.Shared;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgram.NetFramework
{
    public class InputArgumentsTest
    {
        [ChangeFirstInputArgumentToIntArrayWithCountAspect(Count = 10)]
        public void MethodWithByRef(ref int[] array)
        {
            Console.WriteLine($"{nameof(MethodWithByRef)} - class:");
            DumpArray(array);
        }

        [ChangeFirstInputArgumentToIntArrayWithCountAspect(Count = 10)]
        [ChangeFirstInputArgumentToIntArrayWithCountAspect(Count = 20)]
        public void MethodWithByRefTwice(ref int[] array)
        {
            Console.WriteLine($"{nameof(MethodWithByRefTwice)} - class:");
            DumpArray(array);
        }

        [ChangeFirstInputArgumentTo42IfNullAspect]
        public void MethodWithByValue42(object value)
        {
            Console.WriteLine($"{nameof(MethodWithByValue42)} - class - value: {value}");
        }

        [ChangeFirstInputArgumentToExceptionAspect]
        public void MethodWithByValueException(object value)
        {
            Console.WriteLine($"{nameof(MethodWithByValueException)} - class - value: {value}");
        }

        public static void DumpArray(IReadOnlyCollection<int> array)
        {
            Console.WriteLine($"array has length: {array.Count}");
            foreach (var entry in array)
                Console.WriteLine($"entry: {entry}");
        }
    }
}