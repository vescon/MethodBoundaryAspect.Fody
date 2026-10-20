using FluentAssertions;
using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Reflection;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// The attributes of the runtime library can only be applied where the weaver reads them.
    /// </summary>
    public class AttributeUsageTests
    {
        private static AttributeUsageAttribute GetUsage(Type attributeType) =>
            attributeType.GetCustomAttribute<AttributeUsageAttribute>(inherit: false);

        [Fact]
        public void DisableWeavingShouldBeAppliedToWhatTheWeaverReads()
        {
            var usage = GetUsage(typeof(DisableWeavingAttribute));

            usage.ValidOn.Should().HaveFlag(AttributeTargets.Assembly)
                .And.HaveFlag(AttributeTargets.Module)
                .And.HaveFlag(AttributeTargets.Class)
                .And.HaveFlag(AttributeTargets.Struct)
                .And.HaveFlag(AttributeTargets.Method)
                .And.HaveFlag(AttributeTargets.Property)
                .And.HaveFlag(AttributeTargets.Event);
            usage.ValidOn.Should().NotHaveFlag(AttributeTargets.Parameter)
                .And.NotHaveFlag(AttributeTargets.Field)
                .And.NotHaveFlag(AttributeTargets.ReturnValue);
            usage.AllowMultiple.Should().BeFalse();
        }

        [Fact]
        public void AspectOrderIndexShouldBeAppliedToWhatTheWeaverReads()
        {
            var usage = GetUsage(typeof(AspectOrderIndexAttribute));

            usage.ValidOn.Should().HaveFlag(AttributeTargets.Assembly)
                .And.HaveFlag(AttributeTargets.Class)
                .And.HaveFlag(AttributeTargets.Struct)
                .And.HaveFlag(AttributeTargets.Method)
                .And.HaveFlag(AttributeTargets.Property)
                .And.HaveFlag(AttributeTargets.Event);
            usage.ValidOn.Should().NotHaveFlag(AttributeTargets.Parameter)
                .And.NotHaveFlag(AttributeTargets.Field);
            usage.AllowMultiple.Should().BeTrue("one index per aspect type");
        }

        [Theory]
        [InlineData(typeof(AllowChangingInputArgumentsAttribute))]
        [InlineData(typeof(AspectSkipPropertiesAttribute))]
        public void AspectMarkersShouldOnlyBeAppliedOnceToClasses(Type attributeType)
        {
            var usage = GetUsage(attributeType);

            usage.ValidOn.Should().Be(AttributeTargets.Class);
            usage.AllowMultiple.Should().BeFalse();
            usage.Inherited.Should().BeTrue("the weaver reads the markers of the base aspects");
        }
    }
}
