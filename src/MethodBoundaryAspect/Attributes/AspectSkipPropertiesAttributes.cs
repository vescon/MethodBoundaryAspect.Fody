using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    /// <summary>
    /// Put on an aspect class: the aspect is not woven into property getters and setters when it is applied to a
    /// class or an assembly. Inherited by derived aspects, which can override it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class AspectSkipPropertiesAttribute : Attribute
    {
        public AspectSkipPropertiesAttribute(bool skipProperties)
        {
            SkipProperties = skipProperties;
        }

        public bool SkipProperties { get; set; }
    }
}
