using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class OverwriteReturnValueWithRefIntAspect : OnMethodBoundaryAspect
    {
        public static int AspectValue = 0;

        public override void OnExit(MethodExecutionArgs arg)
        {
            arg.ReturnValue = AspectValue;
        }
    }
}
