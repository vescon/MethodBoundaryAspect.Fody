using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class NonStandardProperties
    {
        [FirstAspect]
        public string Property_WithDouble_Underscore { get; set; }
    }
}