using System;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Shared.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    public class SkipPEVerify : Attribute
    {
    }
}
