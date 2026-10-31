using System;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class RefArgumentAfterExceptionMethods
    {
        [RecordRefArguments]
        public void AssignAndThrow(ref int value)
        {
            value = 5;
            throw new InvalidOperationException("failed");
        }
    }
}
