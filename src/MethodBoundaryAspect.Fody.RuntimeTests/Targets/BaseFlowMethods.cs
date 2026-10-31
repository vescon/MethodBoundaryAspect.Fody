using System;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class BaseFlowMethods
    {
        public bool BodyRan;

        [DerivedFlow]
        public int Skipped()
        {
            BodyRan = true;
            return 1;
        }

        [KeepArguments]
        public void WithoutParameters()
        {
        }

        [KeepArguments]
        public void WithParameter(int value)
        {
        }
    }
}
