using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.MultipleAspects.FailTests
{
#pragma warning disable 0618
    [ProvideAspectRole(TestRoles.Third)]
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, TestRoles.Third)]
#pragma warning restore 0618
    public class RoleAndSameOrderingAspect : OnMethodBoundaryAspect
    {
    }
}