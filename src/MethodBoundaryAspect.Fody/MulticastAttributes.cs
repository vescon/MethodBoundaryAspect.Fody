using System;

namespace MethodBoundaryAspect.Fody
{
    [Flags]
    public enum MulticastAttributes
    {
        Default = 0,
        Private = 2,
        Protected = 4,
        Internal = 8,
        InternalAndProtected = 16,
        InternalOrProtected = 32,
        Public = 64,
        AnyVisibility = 64,
    }
}