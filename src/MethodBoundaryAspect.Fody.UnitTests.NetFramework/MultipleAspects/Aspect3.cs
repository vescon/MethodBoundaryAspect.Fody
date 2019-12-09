using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.MultipleAspects
{
    [ProvideAspectRole(TestRoles.Third)]
    public class Aspect3 : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            MultipleAspectsWithOrderIndexMethods.Result += "[Aspect3_OnEntry]";
            arg.MethodExecutionTag = "[Aspect3_OnExit]";
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            MultipleAspectsWithOrderIndexMethods.Result += arg.MethodExecutionTag;
        }
    }
}