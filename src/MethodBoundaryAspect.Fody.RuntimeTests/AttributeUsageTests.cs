using FluentAssertions;
using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Reflection;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// The attributes of the runtime library can be applied everywhere they could in version 3.4.0: the weaver only
    /// reads them in some places, but restricting the usage makes code that compiles today fail to compile.
    /// This is planned for version 4.
    /// </summary>
    public class AttributeUsageTests
    {
        private static AttributeUsageAttribute GetUsage(Type attributeType) =>
            attributeType.GetCustomAttribute<AttributeUsageAttribute>(inherit: false)
            ?? new AttributeUsageAttribute(AttributeTargets.All); // what the compiler assumes without the attribute

        [Theory]
        [InlineData(typeof(DisableWeavingAttribute))]
        [InlineData(typeof(AspectOrderIndexAttribute))]
        [InlineData(typeof(AspectSkipPropertiesAttribute))]
        public void AttributesShouldStillBeAllowedOnAllTargets(Type attributeType)
        {
            GetUsage(attributeType).ValidOn.Should().Be(AttributeTargets.All);
        }

        [Fact]
        public void AspectOrderIndexShouldStillBeAllowedMultipleTimes()
        {
            GetUsage(typeof(AspectOrderIndexAttribute)).AllowMultiple.Should().BeTrue("one index per aspect type");
        }

        [Fact]
        public void AllowChangingInputArgumentsShouldStillBeAllowedMultipleTimesOnClasses()
        {
            var usage = GetUsage(typeof(AllowChangingInputArgumentsAttribute));

            usage.ValidOn.Should().Be(AttributeTargets.Class);
            usage.AllowMultiple.Should().BeTrue();
        }
    }
}
