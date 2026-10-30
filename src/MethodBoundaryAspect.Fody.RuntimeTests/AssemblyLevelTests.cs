using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.AssemblyLevel;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// An aspect applied to the assembly ([assembly: ...]) is woven into the methods of all types, also nested ones
    /// and property accessors, and runs before the aspects of the class and of the method.
    /// </summary>
    [Collection(nameof(AssemblyLevelAttribute))]
    public class AssemblyLevelTests
    {
        public AssemblyLevelTests()
        {
            lock (AssemblyLevelAttribute.Calls)
                AssemblyLevelAttribute.Calls.Clear();
        }

        [Fact]
        public void AspectOfTheAssemblyShouldBeWovenIntoMethodsPropertiesAndNestedTypes()
        {
            var target = new OnlyTheAssemblyAspect();

            target.Method();
            target.Property = 1;
            var value = target.Property;
            new OnlyTheAssemblyAspect.Nested().NestedMethod();

            AssemblyLevelAttribute.Calls.Should().Equal(
                "assembly Method", "assembly set_Property", "assembly get_Property", "assembly NestedMethod");
        }

        [Fact]
        public void AspectsOfTheAssemblyTheClassAndTheMethodShouldRunInThisOrder()
        {
            var target = new AssemblyAndClassAspect();

            target.Both();
            target.ClassOnly();

            AssemblyLevelAttribute.Calls.Should().Equal(
                "assembly Both", "class Both", "method Both",
                "assembly ClassOnly", "class ClassOnly");
        }

        [Fact]
        public void AspectOfTheAssemblyShouldRespectDisableWeavingAndTheNamespaceFilter()
        {
            new ExcludedByAttribute().Method();
            new OutsideTheAssemblyAspectNamespace().Method();

            AssemblyLevelAttribute.Calls.Should().BeEmpty();
        }
    }
}
