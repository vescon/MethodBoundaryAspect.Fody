using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.External.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class ExternalNamedArgumentsMethods
    {
        public static object Result { get; set; }

        [ExternalNamedArgumentsAspect(Field = "external", Tags = new[] { "a", "b" })]
        public void ExternalAspect()
        {
            Result = ExternalNamedArgumentsAspect.LastResult;
        }

        [DerivedExternalNamedArgumentsAspect(Field = "derived")]
        public void DerivedFromExternalAspect()
        {
            Result = ExternalNamedArgumentsAspect.LastResult;
        }
    }
}
