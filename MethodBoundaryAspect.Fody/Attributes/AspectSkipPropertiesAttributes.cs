using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    public class AspectSkipPropertiesAttribute : Attribute
    {
        public AspectSkipPropertiesAttribute(bool skipProperties)
        {
            SkipProperties = skipProperties;
        }

        public bool SkipProperties { get; set; }
    }
}