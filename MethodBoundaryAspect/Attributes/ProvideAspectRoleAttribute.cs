using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    public class ProvideAspectRoleAttribute : Attribute
    {
        public ProvideAspectRoleAttribute(string aspectRole)
        {
            AspectRole = aspectRole;
        }

        public string AspectRole { get; set; }
    }
}