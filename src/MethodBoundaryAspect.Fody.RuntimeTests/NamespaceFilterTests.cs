using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
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

        [Fact]
        public void MethodNameFilterShouldWeaveOnlyTheMatchingMethods()
        {
            var target = new MethodNameFilterMethods();

            target.WeaveThis();
            target.SkipThis();
            target.CallPrivate();
            target.Weaved = 1;
            target.Skipped = 1;
            var weaved = target.Weaved;
            var skipped = target.Skipped;

            // CallPrivate is not woven, WeavePrivate is
            NamespaceFilterRecordingAspect.Calls.Should().Equal("WeaveThis", "WeavePrivate", "set_Weaved", "get_Weaved");
        }

        [Fact]
        public void MethodNameFilterIsNotAnchored()
        {
            var target = new MethodNameFilterNotAnchored();

            target.HasPartInTheMiddle();
            target.Other();

            NamespaceFilterRecordingAspect.Calls.Should().Equal("HasPartInTheMiddle");
        }

        [Fact]
        public void TypeNameFilterShouldMatchTheNameOfAGenericTypeWithTheNumberOfTypeParameters()
        {
            new TypeNameFilterGeneric<int>().Matching();
            new TypeNameFilterGenericWithoutArity<int>().NotMatching();

            NamespaceFilterRecordingAspect.Calls.Should().Equal("Matching");
        }
    }
}
