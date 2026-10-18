using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// https://github.com/vescon/MethodBoundaryAspect.Fody/issues/135
    /// Weaving OnException into an async method produced an InvalidProgramException in Release builds
    /// when short form branches of MoveNext no longer reached their targets.
    /// </summary>
    public class Issue135Tests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AsyncMethodWithManyInstructionsShouldNotThrow(bool flag)
        {
            var target = new Issue135Methods();

            Func<Task> action = () => target.PaddedBeforeAwait(flag);

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AsyncMethodWithLoopShouldReturnResult()
        {
            var target = new Issue135Methods();

            var result = await target.PaddedInLoop(4);

            result.Should().Be(6);
        }

        [Fact]
        public async Task EarlyReturnShouldWork()
        {
            var target = new Issue135Methods();

            (await target.EarlyReturnWithPadding(true)).Should().Be(1);
            (await target.EarlyReturnWithPadding(false)).Should().Be(2);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PaddedBranchesShouldNotThrow(bool flag)
        {
            var target = new Issue135Methods();

            await target.PaddedElseAfterAwait(flag);
            await target.PaddedWithFinally(flag);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EarlyReturnInPaddedBlockShouldNotThrow(bool flag)
        {
            var target = new Issue135Methods();

            await target.EarlyReturn125Bytes(flag);
            await target.EarlyReturn126Bytes(flag);
            await target.EarlyReturn127Bytes(flag);
        }
    }
}
