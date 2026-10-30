using System;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class AsyncTagMethods
    {
        [ExitFlag]
        public async Task OneAspect(Task gate)
        {
            await gate;
            throw new InvalidOperationException("failed");
        }

        [ExitFlag, OtherAspectWithTag]
        [AspectOrderIndex(typeof(ExitFlagAttribute), 1), AspectOrderIndex(typeof(OtherAspectWithTagAttribute), 2)]
        public async Task TwoAspects(Task gate)
        {
            await gate;
            throw new InvalidOperationException("failed");
        }
    }
}
