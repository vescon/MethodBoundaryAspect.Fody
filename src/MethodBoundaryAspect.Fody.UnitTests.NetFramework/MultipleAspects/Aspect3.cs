using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.MultipleAspects
{
#pragma warning disable 0618
    [ProvideAspectRole(TestRoles.Third)]
#pragma warning restore 0618
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