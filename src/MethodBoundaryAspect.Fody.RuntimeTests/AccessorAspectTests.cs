using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// An aspect on the get or set accessor of a property itself, not on the property, is woven into that accessor.
    /// </summary>
    [Collection(nameof(AccessorAspectAttribute))]
    public class AccessorAspectTests
    {
        public AccessorAspectTests()
        {
            lock (AccessorAspectAttribute.Calls)
                AccessorAspectAttribute.Calls.Clear();
        }

        [Fact]
        public void AspectOnTheGetterOnlyShouldBeCalledForTheGetter()
        {
            var target = new AccessorAspectMethods();

            target.GetterOnly = 1;
            AccessorAspectAttribute.Calls.Should().BeEmpty();

            target.GetterOnly.Should().Be(1);
            AccessorAspectAttribute.Calls.Should().Equal("get_GetterOnly");
        }

        [Fact]
        public void AspectOnTheSetterOnlyShouldBeCalledForTheSetter()
        {
            var target = new AccessorAspectMethods();

            target.SetterOnly = 1;
            AccessorAspectAttribute.Calls.Should().Equal("set_SetterOnly");

            target.SetterOnly.Should().Be(1);
            AccessorAspectAttribute.Calls.Should().Equal("set_SetterOnly");
        }

        [Fact]
        public void AspectsOnBothAccessorsShouldBeCalledOnceEach()
        {
            var target = new AccessorAspectMethods();

            target.Both = 1;
            target.Both.Should().Be(1);

            AccessorAspectAttribute.Calls.Should().Equal("set_Both", "get_Both");
        }

        [Fact]
        public void AspectOnAnAutoPropertyShouldStillWeaveBothAccessors()
        {
            var target = new AccessorAspectMethods();

            target.AutoPropertyOfTheProperty = 1;
            target.AutoPropertyOfTheProperty.Should().Be(1);

            AccessorAspectAttribute.Calls.Should().Equal("set_AutoPropertyOfTheProperty", "get_AutoPropertyOfTheProperty");
        }

        [Fact]
        public void AspectsOnTheAccessorsOfAnAutoPropertyShouldBeCalled()
        {
            var target = new AccessorAspectMethods();

            target.AutoPropertyWithAspectOnAccessors = 1;
            target.AutoPropertyWithAspectOnAccessors.Should().Be(1);

            AccessorAspectAttribute.Calls.Should().Equal(
                "set_AutoPropertyWithAspectOnAccessors", "get_AutoPropertyWithAspectOnAccessors");
        }
    }
}
