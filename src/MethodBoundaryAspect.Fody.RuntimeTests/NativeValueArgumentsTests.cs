using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// Values with a special size or which cannot be boxed, passed through MethodExecutionArgs.Arguments
    /// by an aspect with [AllowChangingInputArguments]:
    /// - a ref/out bool was written back with 4 bytes, overwriting the 3 bytes after it,
    /// - IntPtr/UIntPtr arguments were missing in Arguments, the method threw a NullReferenceException.
    /// (pointers: see PointerArgumentsTests in UnitTests.NetFramework, pointer code is not verifiable)
    /// </summary>
    [Collection(nameof(NativeValueArgumentsAspect))]
    public class NativeValueArgumentsTests
    {
        public NativeValueArgumentsTests()
        {
            NativeValueArgumentsAspect.Arguments.Clear();
            NativeValueArgumentsAspect.Replacement = null;
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void RefBoolShouldOnlyWriteTheBool(bool oldValue, bool newValue)
        {
            var value = new BoolFollowedByBytes { Flag = oldValue, Byte1 = 0xAA, Byte2 = 0xBB, Byte3 = 0xCC };

            new NativeValueMethods().SetRefBool(ref value.Flag, newValue);

            value.Flag.Should().Be(newValue);
            new[] { value.Byte1, value.Byte2, value.Byte3 }.Should().Equal(0xAA, 0xBB, 0xCC);
        }

        [Fact]
        public void OutBoolShouldOnlyWriteTheBool()
        {
            var value = new BoolFollowedByBytes { Byte1 = 0xAA, Byte2 = 0xBB, Byte3 = 0xCC };

            new NativeValueMethods().SetOutBool(out value.Flag);

            value.Flag.Should().BeTrue();
            new[] { value.Byte1, value.Byte2, value.Byte3 }.Should().Equal(0xAA, 0xBB, 0xCC);
        }

        [Fact]
        public void IntPtrShouldBePassedToTheAspectAndMethod()
        {
            new NativeValueMethods().IntPtrToInt64(new IntPtr(42)).Should().Be(42);

            NativeValueArgumentsAspect.Arguments.Should().ContainSingle()
                .Which.Should().Equal(new IntPtr(42));
        }

        [Fact]
        public void UIntPtrShouldBePassedToTheAspectAndMethod()
        {
            new NativeValueMethods().UIntPtrToUInt64(new UIntPtr(42)).Should().Be(42);

            NativeValueArgumentsAspect.Arguments.Should().ContainSingle()
                .Which.Should().Equal(new UIntPtr(42));
        }

        [Fact]
        public void IntPtrChangedByTheAspectShouldBePassedToTheMethod()
        {
            NativeValueArgumentsAspect.Replacement = new IntPtrReplacement { Index = 0, Value = new IntPtr(7) };

            new NativeValueMethods().IntPtrToInt64(new IntPtr(42)).Should().Be(7);
        }

        [Fact]
        public void RefIntPtrShouldBeWrittenBack()
        {
            var value = new IntPtr(21);

            new NativeValueMethods().DoubleRefIntPtr(ref value);

            value.Should().Be(new IntPtr(42));
        }

        [Fact]
        public void BoolArrayAspectArgumentShouldKeepItsValues()
        {
            new NativeValueMethods().WithBoolArrayAspect();

            BoolArrayAspect.LastValues.Should().Equal(true, false, true, true);
        }
    }
}
