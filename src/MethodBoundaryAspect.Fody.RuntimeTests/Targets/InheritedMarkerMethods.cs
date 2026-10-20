using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class InheritedMarkerMethods
    {
        [ChangingInputDerived]
        public int Derived(int value) => value;

        [ChangingInputDerivedTwiceAspect]
        public int DerivedTwice(int value) => value;
    }

    [SkipPropertiesDerivedAspect]
    public class InheritedSkipPropertiesDerived
    {
        public int Property { get; set; }

        public void Method()
        {
        }
    }

    [SkipPropertiesOverriddenAspect]
    public class InheritedSkipPropertiesOverridden
    {
        public int Property { get; set; }

        public void Method()
        {
        }
    }
}
