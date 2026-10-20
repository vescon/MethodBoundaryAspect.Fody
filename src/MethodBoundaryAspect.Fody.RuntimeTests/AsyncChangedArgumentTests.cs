using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// [AllowChangingInputArguments] was ignored for async methods: the changed arguments never reached the
    /// state machine.
    /// </summary>
    [Collection(nameof(NameRecordingAspect))]
    public class AsyncChangedArgumentTests
    {
        private readonly AsyncChangedArgumentMethods _target = new AsyncChangedArgumentMethods();

        public AsyncChangedArgumentTests()
        {
            NameRecordingAspect.Calls.Clear();
        }

        [Fact]
        public async Task ChangedArgumentShouldBePassedToTheAsyncMethod()
        {
            (await _target.Changed(1)).Should().Be(42);
        }

        [Fact]
        public async Task ChangedArgumentShouldBePassedToTheAsyncMethodOfAnInstance()
        {
            (await _target.ChangedOfInstance(1)).Should().Be(142);
        }

        [Fact]
        public async Task ChangedArgumentShouldBePassedToAnAsyncMethodWithoutResult()
        {
            await _target.ChangedWithoutResult(1);

            AsyncChangedArgumentMethods.Result.Should().Be(42);
        }

        [Fact]
        public async Task NullForAValueTypeArgumentShouldPassTheDefaultValue()
        {
            (await _target.NullForInt(5)).Should().Be(0);
            (await _target.NullForSecondArgument("text", Guid.NewGuid())).Should().Be("text" + Guid.Empty);
        }

        [Fact]
        public async Task NullForAGenericArgumentShouldPassTheDefaultValue()
        {
            (await _target.NullForGeneric(5)).Should().Be(0);
            (await _target.NullForGeneric("text")).Should().BeNull();
        }

        [Fact]
        public async Task ArgumentsShouldNotBeChangedWithoutAnAspectChangingThem()
        {
            (await _target.Unchanged(5)).Should().Be(5);
        }
    }
}
