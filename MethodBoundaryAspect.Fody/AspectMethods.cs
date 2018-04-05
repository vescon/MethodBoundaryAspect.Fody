using System;

namespace MethodBoundaryAspect.Fody
{
    [Flags]
    public enum AspectMethods
    {
        None = 0,
        OnEntry = 1 << 0,
        OnExit = 1 << 1,
        OnException = 1 << 2
    }
}