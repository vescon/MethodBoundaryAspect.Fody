using System.Linq;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    [AllowChangingInputArguments]
    public class ChangeFirstInputArgumentToIntArrayAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.Arguments[0] = Enumerable
                .Range(0, 10)
                .ToArray();
        }
    }
}