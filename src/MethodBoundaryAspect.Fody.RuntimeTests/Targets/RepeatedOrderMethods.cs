using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    [PlainOrder, SecondOrder]
    [AspectOrderIndex(typeof(PlainOrderAttribute), 1), AspectOrderIndex(typeof(SecondOrderAttribute), 2)]
    public class RepeatedOrderMethods
    {
        // the same aspect on the class and on the method has the same order index
        [PlainOrder]
        public void AspectOnClassAndMethod()
        {
        }

        public void ClassOnly()
        {
        }
    }
}
