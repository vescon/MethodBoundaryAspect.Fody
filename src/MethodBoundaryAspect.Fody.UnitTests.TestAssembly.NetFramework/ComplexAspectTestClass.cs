using System;
using System.Collections.Generic;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Shared.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class ComplexAspectTestClass
    {
        public static string Result { get; set; }

        [ComplexAspect(new object[]
            {
                typeof(string),
                32,
                "test",
                new string[] { "test2", "test3" },
                null,
                new int[] { -1, 15 },
                new Type[]
                {
                    typeof(List<>),
                    typeof(IntExampleEnum[])
                },
                new ByteExampleEnum[] { ByteExampleEnum.ByteFirst, ByteExampleEnum.ByteSecond },
                new SByteExampleEnum[] { SByteExampleEnum.SByteFirst, SByteExampleEnum.SByteSecond, SByteExampleEnum.SByteThird },
                new ShortExampleEnum[] { ShortExampleEnum.ShortFirst, ShortExampleEnum.ShortSecond, ShortExampleEnum.ShortThird },
                new UShortExampleEnum[] { UShortExampleEnum.UShortFirst, UShortExampleEnum.UShortSecond },
                new IntExampleEnum[] { IntExampleEnum.IntFirst, IntExampleEnum.IntSecond, IntExampleEnum.IntThird },
                new UIntExampleEnum[] { UIntExampleEnum.UIntFirst, UIntExampleEnum.UIntSecond },
                new ULongExampleEnum[] { ULongExampleEnum.ULongFirst, ULongExampleEnum.ULongSecond, ULongExampleEnum.ULongThird, ULongExampleEnum.ULongFourth },
                new LongExampleEnum[] { LongExampleEnum.LongFirst, LongExampleEnum.LongSecond, LongExampleEnum.LongThird, LongExampleEnum.LongFourth, LongExampleEnum.LongFifth },
                new object[]
                {
                    typeof(string),
                    32,
                    "test4",
                    new string[] { "test5", "test6" },
                    null,
                    new int[] { 1, 19 },
                    new ByteExampleEnum[] { ByteExampleEnum.ByteFirst, ByteExampleEnum.ByteSecond },
                    new SByteExampleEnum[] { SByteExampleEnum.SByteFirst, SByteExampleEnum.SByteSecond, SByteExampleEnum.SByteThird },
                    new ShortExampleEnum[] { ShortExampleEnum.ShortFirst, ShortExampleEnum.ShortSecond, ShortExampleEnum.ShortThird },
                    new UShortExampleEnum[] { UShortExampleEnum.UShortFirst, UShortExampleEnum.UShortSecond },
                    new IntExampleEnum[] { IntExampleEnum.IntFirst, IntExampleEnum.IntSecond, IntExampleEnum.IntThird },
                    new UIntExampleEnum[] { UIntExampleEnum.UIntFirst, UIntExampleEnum.UIntSecond },
                    new ULongExampleEnum[] { ULongExampleEnum.ULongFirst, ULongExampleEnum.ULongSecond, ULongExampleEnum.ULongThird, ULongExampleEnum.ULongFourth },
                    new LongExampleEnum[] { LongExampleEnum.LongFirst, LongExampleEnum.LongSecond, LongExampleEnum.LongThird, LongExampleEnum.LongFourth, LongExampleEnum.LongFifth },
                    ULongExampleEnum.ULongThird
                }
            })]
        public void TestComplexAspect() { }

        [ComplexAspect(ByteExampleEnum.ByteFirst)] public void ByteSingleEnumFirst() { }
        [ComplexAspect(ByteExampleEnum.ByteSecond)] public void ByteSingleEnumSecond() { }

        [ComplexAspect(SByteExampleEnum.SByteFirst)] public void SByteSingleEnumFirst() { }
        [ComplexAspect(SByteExampleEnum.SByteSecond)] public void SByteSingleEnumSecond() { }
        [ComplexAspect(SByteExampleEnum.SByteThird)] public void SByteSingleEnumThird() { }

        [ComplexAspect(ShortExampleEnum.ShortFirst)] public void ShortSingleEnumFirst() { }
        [ComplexAspect(ShortExampleEnum.ShortSecond)] public void ShortSingleEnumSecond() { }
        [ComplexAspect(ShortExampleEnum.ShortThird)] public void ShortSingleEnumThird() { }

        [ComplexAspect(UShortExampleEnum.UShortFirst)] public void UShortSingleEnumFirst() { }
        [ComplexAspect(UShortExampleEnum.UShortSecond)] public void UShortSingleEnumSecond() { }

        [ComplexAspect(IntExampleEnum.IntFirst)] public void IntSingleEnumFirst() { }
        [ComplexAspect(IntExampleEnum.IntSecond)] public void IntSingleEnumSecond() { }
        [ComplexAspect(IntExampleEnum.IntThird)] public void IntSingleEnumThird() { }

        [ComplexAspect(UIntExampleEnum.UIntFirst)] public void UIntSingleEnumFirst() { }
        [ComplexAspect(UIntExampleEnum.UIntSecond)] public void UIntSingleEnumSecond() { }

        [ComplexAspect(LongExampleEnum.LongFirst)] public void LongSingleEnumFirst() { }
        [ComplexAspect(LongExampleEnum.LongSecond)] public void LongSingleEnumSecond() { }
        [ComplexAspect(LongExampleEnum.LongThird)] public void LongSingleEnumThird() { }
        [ComplexAspect(LongExampleEnum.LongFourth)] public void LongSingleEnumFourth() { }
        [ComplexAspect(LongExampleEnum.LongFifth)] public void LongSingleEnumFifth() { }

        [ComplexAspect(ULongExampleEnum.ULongFirst)] public void ULongSingleEnumFirst() { }
        [ComplexAspect(ULongExampleEnum.ULongSecond)] public void ULongSingleEnumSecond() { }
        [ComplexAspect(ULongExampleEnum.ULongThird)] public void ULongSingleEnumThird() { }
        [ComplexAspect(ULongExampleEnum.ULongFourth)] public void ULongSingleEnumFourth() { }

        [ComplexAspect(new ByteExampleEnum[] { ByteExampleEnum.ByteFirst, ByteExampleEnum.ByteSecond })] public void ArrayEnumByte() { }
        [ComplexAspect(new SByteExampleEnum[] { SByteExampleEnum.SByteFirst, SByteExampleEnum.SByteSecond, SByteExampleEnum.SByteThird })] public void ArrayEnumSByte() { }
        [ComplexAspect(new ShortExampleEnum[] { ShortExampleEnum.ShortFirst, ShortExampleEnum.ShortSecond, ShortExampleEnum.ShortThird })] public void ArrayEnumShort() { }
        [ComplexAspect(new UShortExampleEnum[] { UShortExampleEnum.UShortFirst, UShortExampleEnum.UShortSecond })] public void ArrayEnumUShort() { }
        [ComplexAspect(new IntExampleEnum[] { IntExampleEnum.IntFirst, IntExampleEnum.IntSecond, IntExampleEnum.IntThird })] public void ArrayEnumInt() { }
        [ComplexAspect(new UIntExampleEnum[] { UIntExampleEnum.UIntFirst, UIntExampleEnum.UIntSecond })] public void ArrayEnumUInt() { }
        [ComplexAspect(new ULongExampleEnum[] { ULongExampleEnum.ULongFirst, ULongExampleEnum.ULongSecond, ULongExampleEnum.ULongThird, ULongExampleEnum.ULongFourth })] public void ArrayEnumULong() { }
        [ComplexAspect(new LongExampleEnum[] { LongExampleEnum.LongFirst, LongExampleEnum.LongSecond, LongExampleEnum.LongThird, LongExampleEnum.LongFourth, LongExampleEnum.LongFifth })] public void ArrayEnumLong() { }

        [ComplexAspect((object)ByteExampleEnum.ByteFirst)] public void ByteSingleEnumAsObjectFirst() { }
        [ComplexAspect((object)ByteExampleEnum.ByteSecond)] public void ByteSingleEnumAsObjectSecond() { }
        [ComplexAspect((object)SByteExampleEnum.SByteFirst)] public void SByteSingleEnumAsObjectFirst() { }
        [ComplexAspect((object)SByteExampleEnum.SByteSecond)] public void SByteSingleEnumAsObjectSecond() { }
        [ComplexAspect((object)SByteExampleEnum.SByteThird)] public void SByteSingleEnumAsObjectThird() { }
        [ComplexAspect((object)ShortExampleEnum.ShortFirst)] public void ShortSingleEnumAsObjectFirst() { }
        [ComplexAspect((object)ShortExampleEnum.ShortSecond)] public void ShortSingleEnumAsObjectSecond() { }
        [ComplexAspect((object)ShortExampleEnum.ShortThird)] public void ShortSingleEnumAsObjectThird() { }
        [ComplexAspect((object)UShortExampleEnum.UShortFirst)] public void UShortSingleEnumAsObjectFirst() { }
        [ComplexAspect((object)UShortExampleEnum.UShortSecond)] public void UShortSingleEnumAsObjectSecond() { }
        [ComplexAspect((object)IntExampleEnum.IntFirst)] public void IntSingleEnumAsObjectFirst() { }
        [ComplexAspect((object)IntExampleEnum.IntSecond)] public void IntSingleEnumAsObjectSecond() { }
        [ComplexAspect((object)IntExampleEnum.IntThird)] public void IntSingleEnumAsObjectThird() { }
        [ComplexAspect((object)UIntExampleEnum.UIntFirst)] public void UIntSingleEnumAsObjectFirst() { }
        [ComplexAspect((object)UIntExampleEnum.UIntSecond)] public void UIntSingleEnumAsObjectSecond() { }
        [ComplexAspect((object)LongExampleEnum.LongFirst)] public void LongSingleEnumAsObjectFirst() { }
        [ComplexAspect((object)LongExampleEnum.LongSecond)] public void LongSingleEnumAsObjectSecond() { }
        [ComplexAspect((object)LongExampleEnum.LongThird)] public void LongSingleEnumAsObjectThird() { }
        [ComplexAspect((object)LongExampleEnum.LongFourth)] public void LongSingleEnumAsObjectFourth() { }
        [ComplexAspect((object)LongExampleEnum.LongFifth)] public void LongSingleEnumAsObjectFifth() { }
        [ComplexAspect((object)ULongExampleEnum.ULongFirst)] public void ULongSingleEnumAsObjectFirst() { }
        [ComplexAspect((object)ULongExampleEnum.ULongSecond)] public void ULongSingleEnumAsObjectSecond() { }
        [ComplexAspect((object)ULongExampleEnum.ULongThird)] public void ULongSingleEnumAsObjectThird() { }
        [ComplexAspect((object)ULongExampleEnum.ULongFourth)] public void ULongSingleEnumAsObjectFourth() { }

        [ComplexAspect((object)new ByteExampleEnum[] { ByteExampleEnum.ByteFirst, ByteExampleEnum.ByteSecond })] public void ArrayEnumAsObjectByte() { }
        [ComplexAspect((object)new SByteExampleEnum[] { SByteExampleEnum.SByteFirst, SByteExampleEnum.SByteSecond, SByteExampleEnum.SByteThird })] public void ArrayEnumAsObjectSByte() { }
        [ComplexAspect((object)new ShortExampleEnum[] { ShortExampleEnum.ShortFirst, ShortExampleEnum.ShortSecond, ShortExampleEnum.ShortThird })] public void ArrayEnumAsObjectShort() { }
        [ComplexAspect((object)new UShortExampleEnum[] { UShortExampleEnum.UShortFirst, UShortExampleEnum.UShortSecond })] public void ArrayEnumAsObjectUShort() { }
        [ComplexAspect((object)new IntExampleEnum[] { IntExampleEnum.IntFirst, IntExampleEnum.IntSecond, IntExampleEnum.IntThird })] public void ArrayEnumAsObjectInt() { }
        [ComplexAspect((object)new UIntExampleEnum[] { UIntExampleEnum.UIntFirst, UIntExampleEnum.UIntSecond })] public void ArrayEnumAsObjectUInt() { }
        [ComplexAspect((object)new ULongExampleEnum[] { ULongExampleEnum.ULongFirst, ULongExampleEnum.ULongSecond, ULongExampleEnum.ULongThird, ULongExampleEnum.ULongFourth })] public void ArrayEnumAsObjectULong() { }
        [ComplexAspect((object)new LongExampleEnum[] { LongExampleEnum.LongFirst, LongExampleEnum.LongSecond, LongExampleEnum.LongThird, LongExampleEnum.LongFourth, LongExampleEnum.LongFifth })] public void ArrayEnumAsObjectLong() { }

        [ComplexAspect(new object[] { ByteExampleEnum.ByteFirst, ByteExampleEnum.ByteSecond })] public void ArrayOfEnumsAsObjectArrayByte() { }
        [ComplexAspect(new object[] { SByteExampleEnum.SByteFirst, SByteExampleEnum.SByteSecond, SByteExampleEnum.SByteThird })] public void ArrayOfEnumsAsObjectArraySByte() { }
        [ComplexAspect(new object[] { ShortExampleEnum.ShortFirst, ShortExampleEnum.ShortSecond, ShortExampleEnum.ShortThird })] public void ArrayOfEnumsAsObjectArrayShort() { }
        [ComplexAspect(new object[] { UShortExampleEnum.UShortFirst, UShortExampleEnum.UShortSecond })] public void ArrayOfEnumsAsObjectArrayUShort() { }
        [ComplexAspect(new object[] { IntExampleEnum.IntFirst, IntExampleEnum.IntSecond, IntExampleEnum.IntThird })] public void ArrayOfEnumsAsObjectArrayInt() { }
        [ComplexAspect(new object[] { UIntExampleEnum.UIntFirst, UIntExampleEnum.UIntSecond })] public void ArrayOfEnumsAsObjectArrayUInt() { }
        [ComplexAspect(new object[] { ULongExampleEnum.ULongFirst, ULongExampleEnum.ULongSecond, ULongExampleEnum.ULongThird, ULongExampleEnum.ULongFourth })] public void ArrayOfEnumsAsObjectArrayULong() { }
        [ComplexAspect(new object[] { LongExampleEnum.LongFirst, LongExampleEnum.LongSecond, LongExampleEnum.LongThird, LongExampleEnum.LongFourth, LongExampleEnum.LongFifth })] public void ArrayOfEnumsAsObjectArrayLong() { }

        [ComplexAspect((object)new object[] { ByteExampleEnum.ByteFirst, ByteExampleEnum.ByteSecond })] public void ArrayOfEnumsAsObjectArrayAsSingleObjectByte() { }
        [ComplexAspect((object)new object[] { SByteExampleEnum.SByteFirst, SByteExampleEnum.SByteSecond, SByteExampleEnum.SByteThird })] public void ArrayOfEnumsAsObjectArrayAsSingleObjectSByte() { }
        [ComplexAspect((object)new object[] { ShortExampleEnum.ShortFirst, ShortExampleEnum.ShortSecond, ShortExampleEnum.ShortThird })] public void ArrayOfEnumsAsObjectArrayAsSingleObjectShort() { }
        [ComplexAspect((object)new object[] { UShortExampleEnum.UShortFirst, UShortExampleEnum.UShortSecond })] public void ArrayOfEnumsAsObjectArrayAsSingleObjectUShort() { }
        [ComplexAspect((object)new object[] { IntExampleEnum.IntFirst, IntExampleEnum.IntSecond, IntExampleEnum.IntThird })] public void ArrayOfEnumsAsObjectArrayAsSingleObjectInt() { }
        [ComplexAspect((object)new object[] { UIntExampleEnum.UIntFirst, UIntExampleEnum.UIntSecond })] public void ArrayOfEnumsAsObjectArrayAsSingleObjectUInt() { }
        [ComplexAspect((object)new object[] { ULongExampleEnum.ULongFirst, ULongExampleEnum.ULongSecond, ULongExampleEnum.ULongThird, ULongExampleEnum.ULongFourth })] public void ArrayOfEnumsAsObjectArrayAsSingleObjectULong() { }
        [ComplexAspect((object)new object[] { LongExampleEnum.LongFirst, LongExampleEnum.LongSecond, LongExampleEnum.LongThird, LongExampleEnum.LongFourth, LongExampleEnum.LongFifth })] public void ArrayOfEnumsAsObjectArrayAsSingleObjectLong() { }

        [ComplexAspect(new Type[] { typeof(int), typeof(IDictionary<,>), typeof(IntExampleEnum), typeof(IntExampleEnum[]), typeof(IEnumerable<IntExampleEnum[]>) })]
        public void TestComplexAspectTypeArray() { }

        [ComplexAspect(new string[] { "one", "two" })]
        public void TestComplexAspectStringArray() { }

        [ComplexAspect(new int[] { 0, 42 })]
        public void TestComplexAspectIntArray() { }
    }
}
