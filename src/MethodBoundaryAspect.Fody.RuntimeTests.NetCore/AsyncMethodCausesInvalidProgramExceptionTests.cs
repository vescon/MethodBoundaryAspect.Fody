using FluentAssertions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests.NetCore
{
    /// <summary>
    /// These tests prove a defect where async methods ending with a block cause a System.InvalidProgramException
    /// to be thrown at run-time. These should be woven in a way to not cause an exception at run time. Using the
    /// VerifyAssembly directive _does not_ catch these defects.
    /// </summary>
    public class AsyncMethodCausesInvalidProgramExceptionTests
    {
        [Fact]
        public async Task AsyncIfBlockShouldNotThrow()
        {
            var target = new Targets.AsyncEmptyMethodBoundaryAspectMethods();

            Func<Task> ifAction = () => target.IfEmptyMethodBoundaryAspectMethod(false);

            await ifAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AsyncIfElseBlockShouldNotThrow()
        {
            var target = new Targets.AsyncEmptyMethodBoundaryAspectMethods();

            Func<Task> ifElseAction = () => target.IfElseEmptyMethodBoundaryAspectMethod(false);

            await ifElseAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AsyncTryCatchBlockShouldNotThrow()
        {
            var target = new Targets.AsyncEmptyMethodBoundaryAspectMethods();

            Func<Task> tryCatchAction = () => target.TryCatchEmptyMethodBoundaryAspectMethod();

            await tryCatchAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AsyncTryFinallyBlockShouldNotThrow()
        {
            var target = new Targets.AsyncEmptyMethodBoundaryAspectMethods();

            Func<Task> tryFinallyAction = () => target.TryFinallyEmptyMethodBoundaryAspectMethod();

            await tryFinallyAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AsyncTryCatchFinallyBlockShouldNotThrow()
        {
            var target = new Targets.AsyncEmptyMethodBoundaryAspectMethods();

            Func<Task> tryCatchFinallyAction = () => target.TryCatchFinallyEmptyMethodBoundaryAspectMethod();

            await tryCatchFinallyAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AsyncTryCatchFinallyBlockWReturnShouldNotThrow()
        {
            var target = new Targets.AsyncEmptyMethodBoundaryAspectMethods();

            Func<Task<bool>> tryCatchFinallyFunction = () => target.TryCatchFinallyEmptyMethodBoundaryAspectMethodWResult();

            await tryCatchFinallyFunction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AsyncForeachBlockWReturnShouldNotThrow()
        {
            var target = new Targets.AsyncEmptyMethodBoundaryAspectMethods();

            Func<Task> foreachAction = () => target.ForeachEmptyMethodBoundaryAspectMethod(Enumerable.Empty<object>());

            await foreachAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AsyncSwitchExprBlockWReturnShouldNotThrow()
        {
            var target = new Targets.AsyncEmptyMethodBoundaryAspectMethods();

            Func<Task> switchExprAction = () => target.SwitchExprMethodBoundaryAspectMethod(true);

            await switchExprAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AsyncUsingBlockWReturnShouldNotThrow()
        {
            var target = new Targets.AsyncEmptyMethodBoundaryAspectMethods();

            Func<Task> usingAction = () => target.UsingMethodBoundaryAspectMethod();

            await usingAction.Should().NotThrowAsync();
        }
    }
}
