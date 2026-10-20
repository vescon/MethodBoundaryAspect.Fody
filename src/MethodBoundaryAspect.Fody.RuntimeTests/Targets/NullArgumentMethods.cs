using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class NullArgumentMethods
    {
        [NullArgument]
        public int Int(int value) => value;

        [NullArgument(Index = 1)]
        public Guid Struct(string text, Guid value) => value;

        [NullArgument]
        public DayOfWeek Enum(DayOfWeek value) => value;

        [NullArgument]
        public int Ref(ref int value)
        {
            value++;
            return value;
        }

        [NullArgument]
        public T Generic<T>(T value) => value;

        [NullArgument]
        public string String(string value) => value;
    }
}
