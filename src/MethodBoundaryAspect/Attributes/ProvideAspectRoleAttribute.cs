using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    [Obsolete("Use AspectOrderIndexAttribute to define the execution order of multiple aspects. Aspect roles will be removed in version 4.")]
    public class ProvideAspectRoleAttribute : Attribute
    {
        public ProvideAspectRoleAttribute(string aspectRole)
        {
            AspectRole = aspectRole;
        }

        public string AspectRole { get; set; }
    }
}