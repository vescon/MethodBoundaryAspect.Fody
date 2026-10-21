using System;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// The values of <c>MethodBoundaryAspect.Fody.Attributes.MulticastAttributes</c> of the runtime library, which the
    /// weaver does not reference: the aspect attribute holds the number. Both have to stay the same.
    /// </summary>
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
        AnyVisibility = Private | Protected | Internal | InternalAndProtected | InternalOrProtected | Public,
    }
}