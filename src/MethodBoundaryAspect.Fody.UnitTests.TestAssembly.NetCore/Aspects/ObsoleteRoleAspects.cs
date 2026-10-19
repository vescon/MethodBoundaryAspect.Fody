#pragma warning disable 0618
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects
{
    [ProvideAspectRole("RoleOnly")]
    public class RoleOnlyAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
        }
    }

    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, "RoleOnly")]
    public class RoleDependencyOnlyAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
        }
    }
}
