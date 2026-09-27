using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// MethodExecutionArgs properties which are not used by any aspect of a method are not provided at runtime.
    /// The weaved methods have to behave the same, except that a skipped method body without a return value
    /// set by the aspect returns the default value.
    /// </summary>
    public class ExecutionArgsUsageTests
    {
        private readonly ExecutionArgsUsageMethods _target = new ExecutionArgsUsageMethods();

        public ExecutionArgsUsageTests()
        {
            ExecutionArgsUsageRecorder.Calls.Clear();
        }

        [Fact]
        public void MethodOnlyAspectShouldGetMethodAndKeepResult()
        {
            _target.Add(1, 2).Should().Be(3);

            ExecutionArgsUsageRecorder.Calls.Should().Equal("OnEntry Add", "OnExit Add");
        }

        [Fact]
        public void MethodOnlyAspectShouldKeepRefAndOutArguments()
        {
            var value = 1;

            _target.RefAndOut(ref value, out var text);

            value.Should().Be(2);
            text.Should().Be("changed");
        }

        [Fact]
        public void MethodOnlyAspectShouldGetException()
        {
            Action call = () => _target.Throw();

            call.Should().Throw<InvalidOperationException>().WithMessage("An exception");
            ExecutionArgsUsageRecorder.Calls.Should().Equal("OnEntry Throw", "OnException Throw An exception");
        }

        [Fact]
        public void MethodOnlyAspectShouldWorkForGenericMethods()
        {
            ExecutionArgsUsageMethods.Generic("value").Should().Be("value");
            new ExecutionArgsUsageGeneric<int>().OpenGeneric(1, 2.5).Should().Be(2.5);

            ExecutionArgsUsageRecorder.Calls.Should().Equal(
                "OnEntry Generic", "OnExit Generic",
                "OnEntry OpenGeneric", "OnExit OpenGeneric");
        }

        [Fact]
        public async Task MethodOnlyAspectShouldWorkForAsyncMethods()
        {
            (await _target.Async(21)).Should().Be(42);

            ExecutionArgsUsageRecorder.Calls.Should().Contain("OnEntry Async");
        }

        [Fact]
        public void ArgumentsAspectShouldGetArguments()
        {
            _target.Arguments(1, null).Should().Be(1);

            ExecutionArgsUsageRecorder.Calls.Should().Equal("Arguments [1, null]");
        }

        [Fact]
        public void ReturnValueAspectShouldGetReturnValue()
        {
            _target.ReturnValue(21).Should().Be(42);

            ExecutionArgsUsageRecorder.Calls.Should().Equal("Returned 42");
        }

        [Fact]
        public void OverwrittenReturnValueShouldBeReturned()
        {
            _target.OverwrittenReturnValue(1).Should().Be(42);
        }

        [Fact]
        public void NotOverwrittenReturnValueShouldBeKept()
        {
            _target.OverwriteConditionally(1).Should().Be(42);
            _target.KeepConditionally(1).Should().Be(1);
        }

        [Fact]
        public void InstanceAspectShouldGetInstance()
        {
            _target.Instance().Should().Be(1);

            ExecutionArgsUsageRecorder.Calls.Should().Equal("Instance ExecutionArgsUsageMethods");
        }

        [Fact]
        public void CombinedAspectsShouldGetTheUsedPropertiesOfAllAspects()
        {
            _target.Combined(1).Should().Be(2);

            ExecutionArgsUsageRecorder.Calls.Should().Equal(
                "OnEntry Combined",
                "Arguments [1]",
                "Instance ExecutionArgsUsageMethods",
                "Returned 2",
                "OnExit Combined");
        }

        [Fact]
        public void SkippedMethodWithoutReturnValueShouldReturnDefault()
        {
            _target.SkippedInt().Should().Be(0);
            _target.SkippedString().Should().BeNull();
        }

        [Fact]
        public void SwallowedExceptionWithoutReturnValueShouldReturnDefault()
        {
            _target.SwallowedInt().Should().Be(0);
        }

        [Fact]
        public void OptedOutAspectShouldReadBackTheReturnValue()
        {
            // the optimization is disabled in FodyWeavers.xml: the (not set) return value is read back
            // from MethodExecutionArgs.ReturnValue, null can't be unboxed to int
            Action call = () => _target.OptOutSkippedInt();

            call.Should().Throw<NullReferenceException>();
        }
    }
}
