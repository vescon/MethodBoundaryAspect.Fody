using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class InheritedMarkerMethods
    {
        [InheritedMarker]
        public int ChangeArgument(int value) => value;
    }

    [InheritedMarker]
    public class InheritedMarkerPropertyClass
    {
        public int Value { get; set; }

        public int Method(int value) => value;
    }
}
