using MethodBoundaryAspect.Fody.Attributes;
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

        [RecordFlow, ReturnInEntryFlow]
        [AspectOrderIndex(typeof(RecordFlowAttribute), 1), AspectOrderIndex(typeof(ReturnInEntryFlowAttribute), 2)]
        public ref int SkippedAfterRecording() => ref _field;

        [RecordFlow, SwallowExceptionFlow]
        [AspectOrderIndex(typeof(RecordFlowAttribute), 1), AspectOrderIndex(typeof(SwallowExceptionFlowAttribute), 2)]
        public ref int SwallowedAfterRecording() => throw new InvalidOperationException("original");

        [SwallowExceptionFlow]
        public int SwallowedByValue() => throw new InvalidOperationException("original");
    }
}
