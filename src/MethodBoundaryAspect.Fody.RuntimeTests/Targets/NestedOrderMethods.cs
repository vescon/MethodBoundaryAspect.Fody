using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

[assembly: AspectOrderIndex(typeof(NestedOrderFirstAttribute), 1)]
[assembly: AspectOrderIndex(typeof(NestedOrderSecondAttribute), 2)]

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class NestedOrderAssemblyOnly
    {
        [NestedOrderFirst, NestedOrderSecond]
        public void Method()
        {
        }
    }

    // an order index of a class overrides the one of the assembly, also for the methods of its nested classes
    [AspectOrderIndex(typeof(NestedOrderFirstAttribute), 20)]
    [AspectOrderIndex(typeof(NestedOrderSecondAttribute), 10)]
    public class NestedOrderOuter
    {
        [NestedOrderFirst, NestedOrderSecond]
        public void OuterMethod()
        {
        }

        public class Nested
        {
            [NestedOrderFirst, NestedOrderSecond]
            public void NestedMethod()
            {
            }

            // the closest class wins
            [AspectOrderIndex(typeof(NestedOrderFirstAttribute), 1)]
            [AspectOrderIndex(typeof(NestedOrderSecondAttribute), 2)]
            public class Deeper
            {
                [NestedOrderFirst, NestedOrderSecond]
                public void DeeperMethod()
                {
                }

                // and the method overrides the class
                [AspectOrderIndex(typeof(NestedOrderFirstAttribute), 4)]
                [AspectOrderIndex(typeof(NestedOrderSecondAttribute), 3)]
                [NestedOrderFirst, NestedOrderSecond]
                public void MethodWithOwnOrder()
                {
                }
            }
        }
    }
}
