using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// An aspect class with [Conditional("SYMBOL")] is only applied where SYMBOL is defined, the compiler leaves out
    /// the attribute otherwise (see README, "Applying an aspect only in some configurations").
    /// </summary>
    [Collection(nameof(ConditionalAspects))]
    public class ConditionalAspectTests
    {
        public ConditionalAspectTests()
        {
            ConditionalAspects.Calls.Clear();
        }

        [Fact]
        public void AspectWithoutDefinedSymbolShouldNotBeWoven()
        {
            var target = new ConditionalMethods();

            target.Omitted().Should().Be(1);
            new OmittedClass().Method().Should().Be(3);

            ConditionalAspects.Calls.Should().BeEmpty();
        }

        [Fact]
        public void AspectWithDefinedSymbolShouldBeWoven()
        {
            new ConditionalMethods().Woven().Should().Be(2);

            ConditionalAspects.Calls.Should().Equal("Defined Woven");
        }
    }
}
