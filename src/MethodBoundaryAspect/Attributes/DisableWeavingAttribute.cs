using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    /// <summary>
    /// Excludes a method, a property (getter and setter), an event (add and remove accessor), a class
    /// (including its nested classes) or a whole assembly from weaving.
    /// </summary>
    // the usage is restricted to where the weaver reads it in version 4
    public class DisableWeavingAttribute : Attribute
    {
    }
}
