using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// https://github.com/vescon/MethodBoundaryAspect.Fody/issues/70: "Global Aspect weaves it's own OnEntry method,
    /// ends up with a StackOverflowException". Aspect classes and their nested classes must not be woven.
    /// </summary>
    [Collection(nameof(Issue70Tests))]
    public class Issue70Tests
    {
        public Issue70Tests()
        {
            Issue70Log.Calls.Clear();
        }

        [Fact]
        public void AspectsShouldNotBeWovenIntoEachOther()
        {
            new Issue70Methods.TwoAspects().Method().Should().Be(42);

            Issue70Log.Calls.Should().BeEquivalentTo("First Method", "Second Method");
            ShouldNotBeWoven(typeof(Issue70Methods.TwoAspects.First));
            ShouldNotBeWoven(typeof(Issue70Methods.TwoAspects.Second));
        }

        [Fact]
        public void DerivedAspectShouldNotBeWovenIntoBaseAspect()
        {
            new Issue70Methods.DerivedAspect().Method().Should().Be(42);

            Issue70Log.Calls.Should().Equal("Derived Method");
            ShouldNotBeWoven(typeof(Issue70Methods.DerivedAspect.Base));
            ShouldNotBeWoven(typeof(Issue70Methods.DerivedAspect.Derived));
        }

        [Fact]
        public async Task AspectShouldNotBeWovenIntoItsNestedClasses()
        {
            new Issue70Methods.AspectWithHelper().Method().Should().Be(42);
            (await new Issue70Methods.AspectWithHelper().MethodAsync()).Should().Be(42);

            Issue70Log.Calls.Should().Equal("WithHelper Method", "WithHelper MethodAsync");
            ShouldNotBeWoven(typeof(Issue70Methods.AspectWithHelper.WithHelper));
            ShouldNotBeWoven(typeof(Issue70Methods.AspectWithHelper.WithHelper.Helper));
        }

        [Fact]
        public void TargetMethodsShouldBeWoven()
        {
            IsWoven(typeof(Issue70Methods.TwoAspects)).Should().BeTrue();
            IsWoven(typeof(Issue70Methods.DerivedAspect)).Should().BeTrue();
            IsWoven(typeof(Issue70Methods.AspectWithHelper)).Should().BeTrue();
        }

        private static void ShouldNotBeWoven(Type type) =>
            IsWoven(type).Should().BeFalse($"{type} is an aspect or nested in an aspect");

        // the sync weaver moves the original body into a $_executor_ method
        private static bool IsWoven(Type type) =>
            type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Any(m => m.Name.StartsWith("$_executor_"));
    }
}
