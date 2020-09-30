using System;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects.Shared
{
    [AllowChangingInputArguments]
    public class ChangeFirstInputArgumentToExceptionAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.Arguments[0] = new InvalidOperationException("Test ex");
        }
    }
}