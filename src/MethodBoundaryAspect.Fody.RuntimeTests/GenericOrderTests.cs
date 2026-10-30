using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// [AspectOrderIndex(typeof(MyAspect&lt;int&gt;), 1)] was compared with the open generic type of the aspect and
    /// never applied: weaving failed with "No order index provided".
    /// </summary>
    [Collection(nameof(GenericOrderLog))]
    public class GenericOrderTests
    {
        public GenericOrderTests()
        {
            GenericOrderLog.Calls.Clear();
        }

        [Fact]
        public void OrderIndexOfAGenericAspectShouldBeUsed()
        {
            new GenericOrderMethods().GenericAfterPlain();

            GenericOrderLog.Calls.Should().Equal("Plain", "Generic<Int32>");
        }

        [Fact]
        public void OrderIndexShouldDistinguishTheTypeArguments()
        {
            new GenericOrderMethods().TwoTypeArguments();

            GenericOrderLog.Calls.Should().Equal("Generic<String>", "Generic<Int32>");
        }

        [Fact]
        public void OrderIndexOfTheOpenGenericTypeShouldStillBeUsed()
        {
            new GenericOrderMethods().OpenGenericType();

            GenericOrderLog.Calls.Should().Equal("Plain", "Generic<Int32>");
        }
    }
}
