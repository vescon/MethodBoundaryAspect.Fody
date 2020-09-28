using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AllowChangingInputArgumentsAttribute : Attribute
    {
    }
}