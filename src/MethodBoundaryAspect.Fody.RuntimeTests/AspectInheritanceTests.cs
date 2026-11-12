using AwesomeAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// The weaver reads the aspects of a method from the method, its class, the classes around it and the assembly.
    /// They are not inherited by a derived class, an override or the implementation of an interface, although the
    /// attribute is inheritable and reflection reports it.
    /// </summary>
    [Collection(nameof(NameRecordingAspect))]
    public class AspectInheritanceTests
    {
        public AspectInheritanceTests()
        {
            NameRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void TheClassAspectShouldHitTheMethodsOfTheClass()
        {
            new AspectInheritanceDerived().OwnMethodOfBase();

            NameRecordingAspect.Calls.Should().Equal(nameof(AspectInheritanceBase.OwnMethodOfBase));
        }

        [Fact]
        public void TheClassAspectShouldNotHitTheMethodsOfADerivedClass()
        {
            var target = new AspectInheritanceDerived();

            target.OwnMethodOfDerived();
            target.ClassAspectOverridden();

            NameRecordingAspect.Calls.Should().BeEmpty();
        }

        [Fact]
        public void TheMethodAspectShouldNotHitTheOverride()
        {
            AspectInheritanceBase target = new AspectInheritanceDerived();

            target.MethodAspectOverridden();

            NameRecordingAspect.Calls.Should().BeEmpty();
        }

        [Fact]
        public void TheAspectOfAnInterfaceShouldNotHitTheImplementation()
        {
            IAspectInheritance target = new AspectInheritanceImplementation();

            target.Run();

            NameRecordingAspect.Calls.Should().BeEmpty();
        }
    }
}
