using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    [AllowChangingInputArguments]
    public class ChangeInputArgumentAspect : OnMethodBoundaryAspect
    {
        public int Index { get; set; }
        public object Value { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.Arguments[Index] = Value;
        }
    }
}