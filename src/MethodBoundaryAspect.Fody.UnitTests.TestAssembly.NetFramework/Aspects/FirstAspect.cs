using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class FirstAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            MultipleAspectsMethods.Result += "[FirstAspect_OnEntry]";
            arg.MethodExecutionTag = "[FirstAspect_OnExit]";
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            MultipleAspectsMethods.Result += arg.MethodExecutionTag;
        }
    }
}