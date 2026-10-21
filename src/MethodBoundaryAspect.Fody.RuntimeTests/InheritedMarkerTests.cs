using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System.Linq;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// [AllowChangingInputArguments] and [AspectSkipProperties] on a base aspect class apply to the derived aspect:
    /// the weaver only read them from the aspect class itself, so the derived aspect silently could not change arguments.
    /// </summary>
    public class InheritedMarkerTests
    {
        [Fact]
        public void AllowChangingInputArgumentsOfTheBaseAspectShouldApply()
        {
            new InheritedMarkerMethods().ChangeArgument(1).Should().Be(42);
        }

        [Fact]
        public void AspectSkipPropertiesOfTheBaseAspectShouldApply()
        {
            var type = typeof(InheritedMarkerPropertyClass);

            type.GetMethod("get_Value").GetCustomAttributes(typeof(System.Diagnostics.DebuggerStepThroughAttribute), false)
                .Should().BeEmpty("property accessors are skipped");
            type.GetMethods().Any(m => m.Name == "$_executor_get_Value").Should().BeFalse();
            type.GetMethod("Method").GetCustomAttributes(typeof(System.Diagnostics.DebuggerStepThroughAttribute), false)
                .Should().NotBeEmpty("the method is woven");
        }
    }
}
