using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// Aspects defined in another assembly (README: "Aspects in a separate library"), on every runtime.
    /// </summary>
    [Collection(nameof(ExternalCalls))]
    public class ExternalAspectTests
    {
        public ExternalAspectTests()
        {
            ExternalCalls.Clear();
        }

        [Fact]
        public void AspectOfAnotherAssemblyShouldBeCalled()
        {
            new ExternalAspectMethods().Stateless().Should().Be(1);

            ExternalCalls.Snapshot().Should().Equal("entry a Stateless", "exit a Stateless");
        }

        [Fact]
        public void AspectOnAClassShouldBeCalledForEveryMethod()
        {
            var target = new ExternalAspectOnClass();
            target.First();
            target.Second();

            ExternalCalls.Snapshot().Should().Equal("entry class First", "exit class First", "entry class Second", "exit class Second");
        }

        [Fact]
        public void AspectWithStateShouldBeCreatedForEveryCall()
        {
            var target = new ExternalAspectMethods();
            target.Counting();
            target.Counting();

            ExternalCalls.Snapshot().Should().Equal("count 1", "count 1");
        }

        [Fact]
        public void GenericAspectOfAnotherAssemblyShouldBeCalledWithItsTypeArgument()
        {
            new ExternalAspectMethods().Generic();
            new ExternalAspectMethods().GenericOfInt();

            ExternalCalls.Snapshot().Should().Equal("generic String", "generic Int32");
        }

        [Fact]
        public void BaseClassOfAnotherAssemblyShouldWorkWithTheDerivedAspect()
        {
            new ExternalAspectMethods().Derived();

            ExternalCalls.Snapshot().Should().Equal("base entry derived", "base exit derived");
        }
    }
}
