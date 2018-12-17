using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DisableWeavingAttribute : Attribute
    {
    }
}
