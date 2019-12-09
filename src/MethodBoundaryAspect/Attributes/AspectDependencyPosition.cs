using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    [Obsolete("Use AspectOrderIndexAttribute to define the execution order of multiple aspects.")]
    public enum AspectDependencyPosition
    {
        Before = -1,
        Any = 0,
        After = 1,
    }
}