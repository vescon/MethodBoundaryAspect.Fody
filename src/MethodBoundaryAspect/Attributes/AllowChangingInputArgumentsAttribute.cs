using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    /// <summary>
    /// Put on an aspect class: the aspect can change the elements of <see cref="MethodExecutionArgs.Arguments"/>
    /// in OnEntry, the changed values are passed to the method. The weaver generates additional code for it.
    /// </summary>
    // the usage is restricted to where the weaver reads it in version 4 (AllowMultiple = false)
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AllowChangingInputArgumentsAttribute : Attribute
    {
    }
}
