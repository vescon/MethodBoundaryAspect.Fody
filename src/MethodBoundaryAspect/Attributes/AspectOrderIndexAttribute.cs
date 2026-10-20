using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    /// <summary>
    /// Defines the order of the aspects of a method: the aspect with the lower index runs OnEntry first,
    /// OnExit and OnException in reverse order. If one aspect of a method has an index, all aspects of the
    /// method need a unique one. An index on the method overrides an index on the class, which overrides
    /// an index on the assembly.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method
        | AttributeTargets.Property | AttributeTargets.Event,
        AllowMultiple = true)]
    public class AspectOrderIndexAttribute : Attribute
    {
        public AspectOrderIndexAttribute(
            Type aspectType,
            int orderIndex)
        {
            AspectType = aspectType;
            OrderIndex = orderIndex;
        }

        /// <summary>
        /// The type of the aspect the index is defined for.
        /// </summary>
        public Type AspectType { get;  }

        public int OrderIndex { get; }
    }
}
