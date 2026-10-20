using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    /// <summary>
    /// Put on an aspect class: the aspect is not woven into property getters and setters when it is applied to a
    /// class or an assembly.
    /// </summary>
    // the usage is restricted to classes in version 4
    public class AspectSkipPropertiesAttribute : Attribute
    {
        public AspectSkipPropertiesAttribute(bool skipProperties)
        {
            SkipProperties = skipProperties;
        }

        public bool SkipProperties { get; set; }
    }
}
