using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.MultipleAspects
{
#pragma warning disable 0618
    [ProvideAspectRole(TestRoles.First)]
#pragma warning restore 0618
    public class Aspect1 : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            MultipleAspectsWithOrderIndexMethods.Result += "[Aspect1_OnEntry]";
            arg.MethodExecutionTag = "[Aspect1_OnExit]";
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            MultipleAspectsWithOrderIndexMethods.Result += arg.MethodExecutionTag;
        }
    }
}