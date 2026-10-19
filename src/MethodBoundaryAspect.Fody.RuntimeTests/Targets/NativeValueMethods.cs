using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Runtime.InteropServices;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    /// <summary>
    /// A bool followed by other bytes, to detect writes of more than one byte into the bool.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BoolFollowedByBytes
    {
        public bool Flag;
        public byte Byte1;
        public byte Byte2;
        public byte Byte3;
    }

    public class NativeValueMethods
    {
        [NativeValueArgumentsAspect]
        public void SetRefBool(ref bool value, bool newValue) => value = newValue;

        [NativeValueArgumentsAspect]
        public void SetOutBool(out bool value) => value = true;

        [NativeValueArgumentsAspect]
        public long IntPtrToInt64(IntPtr value) => value.ToInt64();

        [NativeValueArgumentsAspect]
        public ulong UIntPtrToUInt64(UIntPtr value) => value.ToUInt64();

        [NativeValueArgumentsAspect]
        public void DoubleRefIntPtr(ref IntPtr value) => value = new IntPtr(value.ToInt64() * 2);

        [BoolArrayAspect(true, false, true, true)]
        public void WithBoolArrayAspect()
        {
        }
    }
}
