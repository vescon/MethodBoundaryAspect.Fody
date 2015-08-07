using System;

namespace MethodBoundaryAspect.Fody
{
    [Flags]
    internal enum AspectMethods
    {
        None = 0,
        OnEntry = 1,
        OnExit = 2,
        OnException = 4
    }
}