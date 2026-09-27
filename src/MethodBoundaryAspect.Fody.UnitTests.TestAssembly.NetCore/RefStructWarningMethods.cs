using System;
using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    [OnlyOnEntryAspect]
    public class RefStructWarningMethods
    {
        public int SpanParameter(ReadOnlySpan<char> value) => value.Length;

        public Span<int> SpanReturn(int[] values) => values.AsSpan();

        [DisableWeaving]
        public int SpanParameterWithDisabledWeaving(ReadOnlySpan<char> value) => value.Length;

        public int NoRefStruct(int value) => value;
    }
}
