using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Aspects
{
    [AllowChangingInputArguments]
    public class NativeValueArgumentsAspect : OnMethodBoundaryAspect
    {
        public static readonly List<object[]> Arguments = new List<object[]>();

        public static IntPtrReplacement Replacement { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            lock (Arguments)
                Arguments.Add((object[])arg.Arguments.Clone());

            if (Replacement != null)
                arg.Arguments[Replacement.Index] = Replacement.Value;
        }
    }

    public class IntPtrReplacement
    {
        public int Index { get; set; }
        public object Value { get; set; }
    }

    public class BoolArrayAspect : OnMethodBoundaryAspect
    {
        public static bool[] LastValues;

        private readonly bool[] _values;

        public BoolArrayAspect(params bool[] values)
        {
            _values = values;
        }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            LastValues = _values;
        }
    }
}
