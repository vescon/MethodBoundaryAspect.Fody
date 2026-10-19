using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class ManyAspectsMethods
    {
        [ManyAspectsA, ManyAspectsB, ManyAspectsC, ManyAspectsReturningD]
        public int AddOne(int value) => value + 1;

        [ManyAspectsA, ManyAspectsB, ManyAspectsC, ManyAspectsReturningD]
        public int Throw(int value) => throw new InvalidOperationException("An exception");
    }
}
