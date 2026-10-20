using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// The order index of an enclosing class was handled like an order index of the assembly: with an
    /// [AspectOrderIndex] on the assembly and on the outer class, weaving failed for the methods of the nested classes
    /// ("multiple order indices defined on assembly level").
    /// </summary>
    [Collection(nameof(NestedOrderLog))]
    public class NestedOrderTests
    {
        public NestedOrderTests()
        {
            NestedOrderLog.Calls.Clear();
        }

        [Fact]
        public void OrderIndexOfTheAssemblyShouldBeUsed()
        {
            new NestedOrderAssemblyOnly().Method();

            NestedOrderLog.Calls.Should().Equal("First", "Second");
        }

        [Fact]
        public void OrderIndexOfTheClassShouldOverrideTheAssembly()
        {
            new NestedOrderOuter().OuterMethod();

            NestedOrderLog.Calls.Should().Equal("Second", "First");
        }

        [Fact]
        public void OrderIndexOfTheEnclosingClassShouldOverrideTheAssemblyForNestedClasses()
        {
            new NestedOrderOuter.Nested().NestedMethod();

            NestedOrderLog.Calls.Should().Equal("Second", "First");
        }

        [Fact]
        public void OrderIndexOfTheClosestClassShouldWin()
        {
            new NestedOrderOuter.Nested.Deeper().DeeperMethod();

            NestedOrderLog.Calls.Should().Equal("First", "Second");
        }

        [Fact]
        public void OrderIndexOfTheMethodShouldOverrideTheClasses()
        {
            new NestedOrderOuter.Nested.Deeper().MethodWithOwnOrder();

            NestedOrderLog.Calls.Should().Equal("Second", "First");
        }
    }
}
