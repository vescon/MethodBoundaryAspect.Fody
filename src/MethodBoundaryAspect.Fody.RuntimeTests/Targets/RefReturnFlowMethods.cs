using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class RefReturnFlowMethods
    {
        private int _field;

        [SwallowExceptionFlow]
        public ref int Swallowed() => throw new InvalidOperationException("original");

        [ReturnInEntryFlow]
        public ref int Skipped() => ref _field;

        [SwallowExceptionFlow]
        public int SwallowedByValue() => throw new InvalidOperationException("original");
    }
}
