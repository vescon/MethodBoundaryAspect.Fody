using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    [Obsolete("Use AspectOrderIndexAttribute to define the execution order of multiple aspects.")]
    public enum AspectDependencyAction
    {
        None,
        Order,
    }
}