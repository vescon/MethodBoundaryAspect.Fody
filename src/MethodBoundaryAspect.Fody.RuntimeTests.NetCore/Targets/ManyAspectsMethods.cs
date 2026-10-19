using MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Aspects;
using System;

namespace MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Targets
{
    public class ManyAspectsMethods
    {
        [ManyAspectsA, ManyAspectsB, ManyAspectsC, ManyAspectsReturningD]
        public int AddOne(int value) => value + 1;

        [ManyAspectsA, ManyAspectsB, ManyAspectsC, ManyAspectsReturningD]
        public int Throw(int value) => throw new InvalidOperationException("An exception");
    }
}
