using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.MultipleAspects
{
    [ProvideAspectRole(TestRoles.Second)]
    public class Aspect2 : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            MultipleAspectsWithOrderIndexMethods.Result += "[Aspect2_OnEntry]";
            arg.MethodExecutionTag = "[Aspect2_OnExit]";
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            MultipleAspectsWithOrderIndexMethods.Result += arg.MethodExecutionTag;
        }
    }
}