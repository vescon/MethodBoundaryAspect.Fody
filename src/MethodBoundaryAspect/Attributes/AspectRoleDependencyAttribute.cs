using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    [Obsolete("Use AspectOrderIndexAttribute to define the execution order of multiple aspects.")]
    public class AspectRoleDependencyAttribute : Attribute
    {
        public AspectRoleDependencyAttribute(
            AspectDependencyAction action,
            AspectDependencyPosition position,
            string role)
        {
            Action = action;
            Position = position;
            Role = role;
        }

        public AspectDependencyAction Action { get; set; }
        public AspectDependencyPosition Position { get; set; }
        public string Role { get; set; }
    }
}