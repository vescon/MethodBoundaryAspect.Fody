using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    /// <summary>
    /// The visibilities of methods, used by <see cref="OnMethodBoundaryAspect.AttributeTargetMemberAttributes"/>
    /// to restrict an aspect. The flags can be combined, e.g. <c>Public | Internal</c>.
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