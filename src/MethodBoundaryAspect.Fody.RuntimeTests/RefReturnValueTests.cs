using AwesomeAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// The ReturnValue of a method with a ref return is written through the reference.
    /// </summary>
    public class RefReturnValueTests
    {
        private readonly RefReturnValueMethods _target = new RefReturnValueMethods();

        [Fact]
        public void IfTheReturnValueOfARefReturnIsSet_ThenTheReferencedVariableChanges()
        {
            var result = _target.Writable();

            result.Should().Be(99);
            _target.Field.Should().Be(99);
        }

        [Fact]
        public void IfTheMethodReturnsARefReadonly_ThenTheReadOnlyVariableIsNotChanged()
        {
            var result = _target.ReadOnly();

            result.Should().Be(7);
            _target.ReadOnlyField.Should().Be(7);
        }

        [Fact]
        public void IfTheMethodReturnsARefReadonlyAndIsVirtual_ThenTheReadOnlyVariableIsNotChanged()
        {
            var target = new RefReturnValueVirtualMethods();

            var result = target.ReadOnly();

            result.Should().Be(7);
            target.ReadOnlyField.Should().Be(7);
        }

        [Fact]
        public void IfTheReturnValueOfARefReturnOfAValueTypeIsNull_ThenTheVariableIsNotChanged()
        {
            var result = _target.NullForValueType();

            result.Should().Be(7);
            _target.Field.Should().Be(7);
        }

        [Fact]
        public void IfTheReturnValueOfARefReturnOfAReferenceTypeIsNull_ThenTheVariableIsNull()
        {
            var result = _target.NullForReferenceType();

            result.Should().BeNull();
            _target.Text.Should().BeNull();
        }
    }
}
