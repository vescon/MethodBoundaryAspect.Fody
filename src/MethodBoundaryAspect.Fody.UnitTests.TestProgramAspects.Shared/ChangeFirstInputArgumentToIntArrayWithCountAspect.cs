using System;
using System.Linq;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgramAspects.Shared
{
    [AllowChangingInputArguments]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class ChangeFirstInputArgumentToIntArrayWithCountAspect : OnMethodBoundaryAspect
    {
        public int Count { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.Arguments[0] = Enumerable
                .Range(0, Count)
                .ToArray();
        }
    }
}