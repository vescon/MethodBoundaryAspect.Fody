using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class GenericOrderMethods
    {
        [GenericOrder<int>, PlainOrder]
        [AspectOrderIndex(typeof(GenericOrderAttribute<int>), 2), AspectOrderIndex(typeof(PlainOrderAttribute), 1)]
        public void GenericAfterPlain()
        {
        }

        [GenericOrder<string>, GenericOrder<int>]
        [AspectOrderIndex(typeof(GenericOrderAttribute<string>), 1), AspectOrderIndex(typeof(GenericOrderAttribute<int>), 2)]
        public void TwoTypeArguments()
        {
        }

        // the open generic type is the older notation for an aspect with one type argument
        [GenericOrder<int>, PlainOrder]
        [AspectOrderIndex(typeof(GenericOrderAttribute<>), 2), AspectOrderIndex(typeof(PlainOrderAttribute), 1)]
        public void OpenGenericType()
        {
        }
    }
}
