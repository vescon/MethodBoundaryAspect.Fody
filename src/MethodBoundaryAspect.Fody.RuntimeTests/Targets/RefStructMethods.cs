using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    /// <summary>
    /// Class level aspect which also hits methods using ref structs,
    /// see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/129
    /// </summary>
    [RecordingMethodBoundaryAspect]
    public class RefStructMethods
    {
        public bool SpanParameter(ReadOnlySpan<char> value, int count) => value.Length == count;

        public Span<int> SpanReturn(int[] values) => values.AsSpan(1);

        public void RefSpanParameter(ref Span<int> values) => values = values.Slice(1);

        public int NoRefStruct(int value) => value * 2;
    }

    public ref struct RefStructWithAspect
    {
        private readonly int _value;

        public RefStructWithAspect(int value)
        {
            _value = value;
        }

        [RecordingMethodBoundaryAspect]
        public int InstanceMethod() => _value * 2;

        [RecordingMethodBoundaryAspect]
        public static int StaticMethod(int value) => value * 2;
    }

    [OverwritingMethodBoundaryAspect]
    public class OverwritingRefStructMethods
    {
        public int ChangeArguments(ReadOnlySpan<int> values, int factor) => values[0] * factor;

        public void ChangeRefArgument(ref Span<int> values, int start) => values = values.Slice(start);

        public Span<int> OverwriteReturnValue(int[] values) => values.AsSpan(1);

        public Span<int> SkipBody(int[] values) => values.AsSpan(1);

        public void SkipBodyWithRefSpan(ref Span<int> values, ref int count)
        {
            values = values.Slice(1);
            count = -1;
        }

        public Span<int> SwallowException(int[] values) => throw new InvalidOperationException("boom");
    }
}
