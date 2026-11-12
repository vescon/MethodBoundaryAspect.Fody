using System;
using AwesomeAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// The weaver emits the code for FlowBehavior and MethodExecutionTag only if an aspect uses them, also if it does
    /// that in the OnEntry of its base class. Methods without parameters get the shared empty array.
    /// </summary>
    [Collection(nameof(KeepArgumentsAttribute))]
    public class BaseFlowTests
    {
        [Fact]
        public void BodyShouldBeSkippedIfTheBaseClassSetsTheFlowBehavior()
        {
            var target = new BaseFlowMethods();

            var result = target.Skipped();

            result.Should().Be(42);
            target.BodyRan.Should().BeFalse();
        }

        [Fact]
        public void TagSetInTheBaseClassShouldBeReadInTheDerivedAspect()
        {
            BaseFlowAspect.TagSeenInDerivedEntry = null;

            new BaseFlowMethods().Skipped();

            BaseFlowAspect.TagSeenInDerivedEntry.Should().Be("set in the base class");
        }

        [Fact]
        public void ArgumentsOfAMethodWithoutParametersShouldBeTheSharedEmptyArray()
        {
            new BaseFlowMethods().WithoutParameters();

            KeepArgumentsAttribute.LastArguments.Should().BeSameAs(Array.Empty<object>());
        }

        [Fact]
        public void ArgumentsOfAMethodWithParametersShouldBeANewArray()
        {
            new BaseFlowMethods().WithParameter(5);

            KeepArgumentsAttribute.LastArguments.Should().Equal(5);
        }
    }
}
