using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    // The properties of an extension block (C# 14) exist only in a nested type the compiler generates, their
    // accessors are static methods of this class.
    public static class ExtensionBlockTargets
    {
        extension(string text)
        {
            [OnlyOnEntryAspect]
            public int Size => text.Length;

            [DisableWeaving]
            [OnlyOnEntryAspect]
            public int Unwoven => text.Length;

            public int WithoutAspect => text.Length;

            public int Method([OnlyOnEntryAspect] int parameter) => parameter + text.Length;
        }

        extension<T>(T[] array)
        {
            [OnlyOnEntryAspect]
            public int Count
            {
                get => array.Length;
                set => array[0] = default;
            }

            [OnlyOnEntryAspect]
            public static int Zero => 0;
        }

        // same name and parameter count as an accessor above
        extension(int number)
        {
            public int Size => number;
        }
    }
}
