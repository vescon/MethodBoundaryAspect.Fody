using System;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class TagIsolationMethods
    {
        [TagSetter, TagObserver]
        [AspectOrderIndex(typeof(TagSetterAttribute), 1), AspectOrderIndex(typeof(TagObserverAttribute), 2)]
        public int Sync() => 1;

        [TagSetter, TagObserver]
        [AspectOrderIndex(typeof(TagSetterAttribute), 1), AspectOrderIndex(typeof(TagObserverAttribute), 2)]
        public int Throwing() => throw new InvalidOperationException("failed");

        [TagSetter, TagObserver]
        [AspectOrderIndex(typeof(TagSetterAttribute), 1), AspectOrderIndex(typeof(TagObserverAttribute), 2)]
        public async Task<int> Async()
        {
            await Task.Yield();
            return 1;
        }

        [TagSetter, TagObserver]
        [AspectOrderIndex(typeof(TagSetterAttribute), 1), AspectOrderIndex(typeof(TagObserverAttribute), 2)]
        public async Task AsyncThrowing()
        {
            await Task.Yield();
            throw new InvalidOperationException("failed");
        }
    }
}
