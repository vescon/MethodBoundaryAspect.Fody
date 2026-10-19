using MethodBoundaryAspect.Fody.Attributes;
using System.Linq;

namespace MethodBoundaryAspect.Fody.UnitTests.UnverifiableTestAssembly.NetFramework.Aspects
{
    [AllowChangingInputArguments]
    public class PointerArgumentsAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            PointerMethods.Arguments = string.Join(", ", arg.Arguments.Select(a => a ?? "null"));
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            PointerMethods.ReturnValue = arg.ReturnValue ?? "null";
        }
    }
}
