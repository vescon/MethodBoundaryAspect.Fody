using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests.NetCore
{
    /// <summary>
    /// Nested classes have no namespace of their own in IL, so a NamespaceFilter never matched them.
    /// </summary>
    [Collection(nameof(NamespaceFilterRecordingAspect))]
    public class NamespaceFilterTests
    {
        public NamespaceFilterTests()
        {
            NamespaceFilterRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void NamespaceFilterShouldMatchNestedClasses()
        {
            new NamespaceFilterOuter().Outer();
            new NamespaceFilterOuter.Nested().Inner();
            new NamespaceFilterOuter.Nested.Deeper().Deepest();

            NamespaceFilterRecordingAspect.Calls.Should().Equal("Outer", "Inner", "Deepest");
        }

        [Fact]
        public void NotMatchingNamespaceFilterShouldNotWeaveNestedClasses()
        {
            new NamespaceFilterNotMatching().Outer();
            new NamespaceFilterNotMatching.Nested().Inner();

            NamespaceFilterRecordingAspect.Calls.Should().BeEmpty();
        }
    }
}
