using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class StructSetter : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            Struct.Result = arg.Arguments[0];
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            
        }
    }
}
