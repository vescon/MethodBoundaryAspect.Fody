using MethodBoundaryAspect.Fody.UnitTests.Shared.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.UnverifiableTestAssembly.NetFramework.Aspects;
using System;
using System.Runtime.InteropServices;

// Unmanaged pointers (PointerMethods) are not verifiable: "unmanaged pointers are not verifiable types" and
// "unexpected type on the stack" for every pointer passed to a method. PEVerify checks the whole assembly,
// so the error codes are ignored for the tests of all classes of this (unverifiable) assembly.
[assembly: IgnorePEVerifyCode("801318DE")]
[assembly: IgnorePEVerifyCode("80131854")]

namespace MethodBoundaryAspect.Fody.UnitTests.UnverifiableTestAssembly.NetFramework
{
    /// <summary>
    /// Pointers cannot be boxed into MethodExecutionArgs. They are null in Arguments and ReturnValue,
    /// and passed unchanged to the method, also by an aspect with [AllowChangingInputArguments].
    /// </summary>
    public unsafe class PointerMethods
    {
        public static object Result { get; set; }
        public static string Arguments { get; set; }
        public static object ReturnValue { get; set; }

        public void CallDereference()
        {
            var memory = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                Marshal.WriteInt32(memory, 40);
                var result = Dereference(memory.ToPointer(), 2);
                Result = $"{result} [{Arguments}]";
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        public void CallIdentity()
        {
            var memory = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                var returned = new IntPtr(Identity(memory.ToPointer())) == memory;
                Result = $"{returned} {ReturnValue}";
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        [PointerArgumentsAspect]
        private int Dereference(void* value, int add) => Marshal.ReadInt32(new IntPtr(value)) + add;

        [PointerArgumentsAspect]
        private void* Identity(void* value) => value;
    }
}
