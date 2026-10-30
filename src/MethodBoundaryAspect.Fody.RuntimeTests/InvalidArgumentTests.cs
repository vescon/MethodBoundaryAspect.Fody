using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// An argument of the wrong type set by an aspect fails before the method is called: the exception does not come
    /// from the method, so OnException is not called for it.
    /// </summary>
    public class InvalidArgumentTests
    {
        private readonly InvalidArgumentMethods _target = new InvalidArgumentMethods();

        public InvalidArgumentTests()
        {
            lock (InvalidFirstArgumentAttribute.OnExceptionCalls)
                InvalidFirstArgumentAttribute.OnExceptionCalls.Clear();
        }

        [Fact]
        public void IfAnArgumentByValueHasTheWrongType_ThenOnExceptionIsNotCalled()
        {
            Action call = () => _target.ByValue(1);

            call.Should().Throw<InvalidCastException>();
            _target.Called.Should().BeFalse();
            InvalidFirstArgumentAttribute.OnExceptionCalls.Should().BeEmpty();
        }

        [Fact]
        public void IfAnArgumentByRefHasTheWrongType_ThenOnExceptionIsNotCalled()
        {
            var value = 1;

            Action call = () => _target.ByRef(ref value);

            call.Should().Throw<InvalidCastException>();
            _target.Called.Should().BeFalse();
            InvalidFirstArgumentAttribute.OnExceptionCalls.Should().BeEmpty();
        }
    }
}
