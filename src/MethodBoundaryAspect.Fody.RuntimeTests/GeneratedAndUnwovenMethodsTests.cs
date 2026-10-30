using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    [Collection(nameof(NameRecordingAspect))]
    public class GeneratedAndUnwovenMethodsTests
    {
        public GeneratedAndUnwovenMethodsTests()
        {
            NameRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void TheMembersGeneratedForARecordShouldBeWoven()
        {
            var first = new AspectRecord(1);
            var second = new AspectRecord(1);
            NameRecordingAspect.Calls.Clear();

            first.Equals(second).Should().BeTrue();
            first.ToString().Should().Be("AspectRecord { Value = 1 }");

            NameRecordingAspect.Calls.Should().Contain(new[] { "Equals", "ToString", "PrintMembers", "get_Value" });
        }

#if NETCOREAPP
        [Fact]
        public void ADefaultInterfaceMethodShouldBeWoven()
        {
            IDefaultMethod target = new DefaultMethodImplementation();

            target.Hello();

            NameRecordingAspect.Calls.Should().Equal("Hello");
        }
#endif

        [Fact]
        public void LambdasAndLocalFunctionsShouldNotBeWoven()
        {
            new UnwovenMethods().WithLambdaAndLocalFunction().Should().Be(3);

            NameRecordingAspect.Calls.Should().Equal(nameof(UnwovenMethods.WithLambdaAndLocalFunction));
        }

        [Fact]
        public void TheStaticConstructorShouldNotBeWoven()
        {
            UnwovenMethods.ReadStatic().Should().Be(5);

            NameRecordingAspect.Calls.Should().NotContain(".cctor");
            NameRecordingAspect.Calls.Should().Contain(nameof(UnwovenMethods.ReadStatic));
        }
    }
}
