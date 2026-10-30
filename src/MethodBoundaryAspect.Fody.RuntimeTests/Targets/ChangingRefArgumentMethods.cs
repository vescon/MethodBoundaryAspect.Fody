using System;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class ChangingRefArgumentMethods
    {
        [ChangeFirstArgumentTo99]
        public ref int Pick(ref int value) => ref value;

        [ChangeFirstArgumentTo99]
        public void Increment(ref int value) => value++;

        [ChangeFirstArgumentTo99]
        public void Assign(out int value) => value = 7;

        [ChangeFirstArgumentAndSwallow]
        public void IncrementAndThrow(ref int value)
        {
            value++;
            throw new InvalidOperationException("failed");
        }
    }
}
