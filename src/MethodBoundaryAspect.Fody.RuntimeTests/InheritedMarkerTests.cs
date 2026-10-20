using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    [Collection(nameof(SkipPropertiesBaseAspect))]
    public class InheritedMarkerTests
    {
        public InheritedMarkerTests()
        {
            SkipPropertiesBaseAspect.Calls.Clear();
        }

        [Fact]
        public void AllowChangingInputArgumentsShouldBeInheritedFromTheBaseAspect()
        {
            new InheritedMarkerMethods().Derived(1).Should().Be(42);
        }

        [Fact]
        public void AllowChangingInputArgumentsShouldBeInheritedFromTheBaseOfTheBaseAspect()
        {
            new InheritedMarkerMethods().DerivedTwice(1).Should().Be(42);
        }

        [Fact]
        public void AspectSkipPropertiesShouldBeInheritedFromTheBaseAspect()
        {
            var target = new InheritedSkipPropertiesDerived();

            target.Property = target.Property;
            target.Method();

            SkipPropertiesBaseAspect.Calls.Should().Equal("Method");
        }

        [Fact]
        public void AspectSkipPropertiesOfTheDerivedAspectShouldOverrideTheBaseAspect()
        {
            var target = new InheritedSkipPropertiesOverridden();

            target.Property = target.Property;
            target.Method();

            SkipPropertiesBaseAspect.Calls.Should().Equal("get_Property", "set_Property", "Method");
        }
    }
}
