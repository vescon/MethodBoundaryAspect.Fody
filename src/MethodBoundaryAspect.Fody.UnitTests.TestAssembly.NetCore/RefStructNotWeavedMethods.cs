using System;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    // methods which are not changed by the weaver need no warning about ref structs

    [NoAspectMethodsAspect]
    public class RefStructWithoutAspectMethods
    {
        public int SpanParameter(ReadOnlySpan<char> value) => value.Length;
    }

    [SkipPropertiesAspect]
    public class RefStructPropertyWithSkippedProperties
    {
        private readonly int[] _values = new int[3];

        public Span<int> Values => _values.AsSpan();

        public int Method(int value) => value;
    }
}
