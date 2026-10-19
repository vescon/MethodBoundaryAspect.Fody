using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// Weaving failed for aspects with named arguments for a field declared in a base class
    /// ("Sequence contains no matching element") and for array properties (InvalidCastException),
    /// and for null arrays (NotSupportedException).
    /// </summary>
    [Collection(nameof(NamedArgumentsBaseAspect))]
    public class NamedArgumentsTests
    {
        [Fact]
        public void FieldAndPropertyOfBaseClassShouldBeSet()
        {
            new NamedArgumentsMethods().InheritedFieldAndProperty();

            NamedArgumentsBaseAspect.LastResult.Should().Be("BaseField: base, BaseProperty: 42");
        }

        [Fact]
        public void ArrayArgumentsShouldBeSet()
        {
            new NamedArgumentsMethods().ArrayArguments();

            NamedArgumentsBaseAspect.LastResult.Should().Be(
                "Ctor: [x, y] String; Tags: [a, b]; Numbers: [1, 2, 3]; Kinds: [Second, First]; Types: [Int32, String]; FieldTags: [c]");
        }

        [Fact]
        public void NullArrayArgumentsShouldBeNull()
        {
            new NamedArgumentsMethods().NullArrayArguments();

            NamedArgumentsBaseAspect.LastResult.Should().Be(
                "Ctor: null null; Tags: null; Numbers: null; Kinds: null; Types: null; FieldTags: null");
        }

        [Fact]
        public void EmptyArrayArgumentShouldBeEmpty()
        {
            new NamedArgumentsMethods().EmptyArrayArgument();

            NamedArgumentsBaseAspect.LastResult.Should().Be(
                "Ctor: null null; Tags: []; Numbers: null; Kinds: null; Types: null; FieldTags: null");
        }
    }
}
