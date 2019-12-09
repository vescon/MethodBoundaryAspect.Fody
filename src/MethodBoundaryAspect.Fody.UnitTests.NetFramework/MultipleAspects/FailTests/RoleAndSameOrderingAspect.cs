using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.MultipleAspects.FailTests
{
    [ProvideAspectRole(TestRoles.Third)]
#pragma warning disable 0618
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, TestRoles.Third)]
#pragma warning restore 0618
    public class RoleAndSameOrderingAspect : OnMethodBoundaryAspect
    {
    }
}