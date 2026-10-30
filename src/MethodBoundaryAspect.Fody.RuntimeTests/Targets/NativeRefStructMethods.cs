using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    /// <summary>
    /// TypedReference, ArgIterator and RuntimeArgumentHandle are ref structs, but .NET Framework does not mark them
    /// with IsByRefLikeAttribute.
    /// </summary>
    public class NativeRefStructMethods
    {
        [ReadArguments]
        public static int Typed(TypedReference reference, int count) => count;

        [ReadArguments]
        public static int Handle(RuntimeArgumentHandle handle, int count) => count;
    }
}
