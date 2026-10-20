using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    [Collection(nameof(NameRecordingAspect))]
    public class DisableWeavingTests
    {
        public DisableWeavingTests()
        {
            NameRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void DisableWeavingOnPropertyShouldExcludeGetterAndSetter()
        {
            var target = new DisableWeavingOnPropertyMethods();

            target.Disabled = 1;
            target.DisabledWithOwnAspect = target.Disabled;
            NameRecordingAspect.Calls.Should().BeEmpty();

            target.Enabled = target.DisabledWithOwnAspect;
            NameRecordingAspect.Calls.Should().Equal("set_Enabled");
        }

        [Fact]
        public void DisableWeavingOnGetterShouldOnlyExcludeTheGetter()
        {
            var target = new DisableWeavingOnPropertyMethods();

            target.DisabledGetterOnly = 1;
            target.DisabledGetterOnly.Should().Be(1);

            NameRecordingAspect.Calls.Should().Equal("set_DisabledGetterOnly");
        }
    }
}
