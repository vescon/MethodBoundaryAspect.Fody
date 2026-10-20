using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// A method with a ref return cannot return a default value, so an aspect cannot skip the method body or
    /// swallow an exception. Both throw a NotSupportedException instead of returning a null reference, which would
    /// fail in the caller. The original exception is kept as the inner exception.
    /// </summary>
    public class RefReturnFlowTests
    {
        private readonly RefReturnFlowMethods _target = new RefReturnFlowMethods();

        [Fact]
        public void IfAnExceptionOfAMethodWithRefReturnIsSwallowed_ThenANotSupportedExceptionWithTheOriginalIsThrown()
        {
            Action call = () => _target.Swallowed();

            var exception = call.Should().Throw<NotSupportedException>().Which;
            exception.Message.Should().Contain("ref return");
            exception.InnerException.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("original");
        }

        [Fact]
        public void IfTheMethodBodyWithRefReturnIsSkipped_ThenANotSupportedExceptionIsThrown()
        {
            Action call = () => _target.Skipped();

            call.Should().Throw<NotSupportedException>().Which.Message.Should().Contain("early return");
        }

        [Fact]
        public void IfAnExceptionOfAMethodWithoutRefReturnIsSwallowed_ThenTheDefaultValueIsReturned()
        {
            _target.SwallowedByValue().Should().Be(0);
        }
    }
}
