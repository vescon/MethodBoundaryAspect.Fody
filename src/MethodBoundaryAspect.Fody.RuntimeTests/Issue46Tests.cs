using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// https://github.com/vescon/MethodBoundaryAspect.Fody/issues/46
    /// "Weaving fails with aspect inheritance": an aspect derived from another aspect which overrides
    /// OnException and calls base.OnException.
    /// </summary>
    public class Issue46Tests
    {
        private readonly InheritedExceptionAspectMethods _target = new InheritedExceptionAspectMethods();

        public Issue46Tests()
        {
            LogExceptionAndThrowAttribute.Calls.Clear();
        }

        [Fact]
        public void BaseAspectShouldLogAndRethrow()
        {
            Action call = () => _target.ThrowAndRethrow();

            call.Should().Throw<InvalidOperationException>().WithMessage("thrown");
            LogExceptionAndThrowAttribute.Calls.Should().Equal(
                "LogExceptionAndThrowAttribute.base ThrowAndRethrow thrown");
        }

        [Fact]
        public void DerivedAspectShouldCallBaseAndSwallow()
        {
            _target.ThrowAndSwallow().Should().Be(-1);

            LogExceptionAndThrowAttribute.Calls.Should().Equal(
                "LogExceptionAndSwallowAttribute.base ThrowAndSwallow thrown",
                "LogExceptionAndSwallowAttribute.derived ThrowAndSwallow");
        }

        [Fact]
        public void DerivedAspectShouldNotInterfereWithoutException()
        {
            _target.NoThrow().Should().Be(42);

            LogExceptionAndThrowAttribute.Calls.Should().BeEmpty();
        }

        [Fact]
        public async Task DerivedAspectShouldCallBaseAndSwallowInAsyncMethod()
        {
            (await _target.ThrowAndSwallowAsync()).Should().Be(-1);

            LogExceptionAndThrowAttribute.Calls.Should().Equal(
                "LogExceptionAndSwallowAttribute.base ThrowAndSwallowAsync thrown",
                "LogExceptionAndSwallowAttribute.derived ThrowAndSwallowAsync");
        }

        [Fact]
        public void DerivedAspectShouldUseInheritedOnException()
        {
            Action call = () => _target.ThrowWithInheritedOnException();

            call.Should().Throw<InvalidOperationException>().WithMessage("thrown");
            LogExceptionAndThrowAttribute.Calls.Should().Equal(
                "LogEntryAndExceptionAttribute.OnEntry ThrowWithInheritedOnException",
                "LogEntryAndExceptionAttribute.base ThrowWithInheritedOnException thrown");
        }
    }
}
