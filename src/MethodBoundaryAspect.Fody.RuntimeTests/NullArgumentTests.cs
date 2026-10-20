using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// An aspect which sets an argument of a value type to null (with [AllowChangingInputArguments]) passes the
    /// default value to the method, like a null ReturnValue returns the default value.
    /// It threw a NullReferenceException when unboxing the argument.
    /// </summary>
    public class NullArgumentTests
    {
        private readonly NullArgumentMethods _target = new NullArgumentMethods();

        [Fact]
        public void NullForAnIntArgumentShouldPassTheDefaultValue()
        {
            _target.Int(5).Should().Be(0);
        }

        [Fact]
        public void NullForAStructArgumentShouldPassTheDefaultValue()
        {
            _target.Struct("text", Guid.NewGuid()).Should().Be(Guid.Empty);
        }

        [Fact]
        public void NullForAnEnumArgumentShouldPassTheDefaultValue()
        {
            _target.Enum(DayOfWeek.Friday).Should().Be(DayOfWeek.Sunday);
        }

        [Fact]
        public void NullForARefArgumentShouldPassTheDefaultValueAndWriteTheResultBack()
        {
            var value = 5;

            _target.Ref(ref value).Should().Be(1);

            value.Should().Be(1);
        }

        [Fact]
        public void NullForAGenericArgumentShouldPassTheDefaultValue()
        {
            _target.Generic(5).Should().Be(0);
            _target.Generic(Guid.NewGuid()).Should().Be(Guid.Empty);
            _target.Generic("text").Should().BeNull();
        }

        [Fact]
        public void NullForAReferenceArgumentShouldBePassed()
        {
            _target.String("text").Should().BeNull();
        }
    }
}
