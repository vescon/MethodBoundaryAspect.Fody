using FluentAssertions;
using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// https://github.com/vescon/MethodBoundaryAspect.Fody/issues/132
    /// MulticastAttributes.AnyVisibility had the same value as MulticastAttributes.Public,
    /// so an aspect with AttributeTargetMemberAttributes = AnyVisibility only weaved public methods.
    /// </summary>
    public class Issue132Tests
    {
        private static readonly string[] AllMethods =
        {
            "CallAll",
            "PrivateMethod",
            "ProtectedMethod",
            "InternalMethod",
            "PrivateProtectedMethod",
            "ProtectedInternalMethod"
        };

        public Issue132Tests()
        {
            VisibilityRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void AnyVisibilityShouldContainAllVisibilities()
        {
            MulticastAttributes.AnyVisibility.Should().Be(
                MulticastAttributes.Private
                | MulticastAttributes.Protected
                | MulticastAttributes.Internal
                | MulticastAttributes.InternalAndProtected
                | MulticastAttributes.InternalOrProtected
                | MulticastAttributes.Public);
        }

        [Fact]
        public void AnyVisibilityShouldWeaveMethodsOfAllVisibilities()
        {
            new AnyVisibilityMethods().CallAll();

            VisibilityRecordingAspect.Calls.Should().Equal(AllMethods);
        }

        [Fact]
        public void DefaultVisibilityShouldWeaveMethodsOfAllVisibilities()
        {
            new DefaultVisibilityMethods().CallAll();

            VisibilityRecordingAspect.Calls.Should().Equal(AllMethods);
        }

        [Fact]
        public void NotConfiguredVisibilityShouldWeaveMethodsOfAllVisibilities()
        {
            new NotConfiguredVisibilityMethods().CallAll();

            VisibilityRecordingAspect.Calls.Should().Equal(AllMethods);
        }

        [Fact]
        public void CombinedVisibilitiesShouldOnlyWeaveMethodsOfTheseVisibilities()
        {
            new PublicAndInternalMethods().CallAll();

            VisibilityRecordingAspect.Calls.Should().Equal("CallAll", "InternalMethod");
        }
    }
}
