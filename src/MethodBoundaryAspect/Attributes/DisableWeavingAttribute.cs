using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    /// <summary>
    /// Excludes a method, a property (getter and setter), an event (add and remove accessor), a class
    /// (including its nested classes) or a whole assembly from weaving.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct
        | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Constructor,
        Inherited = false, AllowMultiple = false)]
    public class DisableWeavingAttribute : Attribute
    {
    }
}
