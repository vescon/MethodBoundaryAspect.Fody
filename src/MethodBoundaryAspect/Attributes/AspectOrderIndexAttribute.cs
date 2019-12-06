using System;

namespace MethodBoundaryAspect.Fody.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class AspectOrderIndexAttribute : Attribute
    {
        public AspectOrderIndexAttribute(
            Type aspectType,
            int orderIndex)
        {
            AspectType = aspectType;
            OrderIndex = orderIndex;
        }

        public Type AspectType { get;  }
        public int OrderIndex { get; }
    }
}