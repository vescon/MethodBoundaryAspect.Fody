using System;

namespace MethodBoundaryAspect.Fody.UnitTests.Shared.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    public class SkipPEVerify : Attribute
    {
    }
}
