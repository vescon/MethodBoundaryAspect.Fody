using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// README: assigning a new array to args.Arguments is ignored, only the elements of the array count.
    /// </summary>
    public class ReplacedArgumentsArrayTests
    {
        [ReplaceArgumentsArray]
        private static int Identity(int value) => value;

        [Fact]
        public void NewArrayAssignedToArgumentsShouldBeIgnored()
        {
            Identity(5).Should().Be(5);
        }
    }
}
