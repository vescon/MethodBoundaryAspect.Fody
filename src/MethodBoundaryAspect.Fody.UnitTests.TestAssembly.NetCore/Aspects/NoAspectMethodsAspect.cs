using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects
{
    /// <summary>
    /// Overrides none of OnEntry, OnExit and OnException, so its methods are not woven.
    /// </summary>
    public class NoAspectMethodsAspect : OnMethodBoundaryAspect
    {
    }
}
