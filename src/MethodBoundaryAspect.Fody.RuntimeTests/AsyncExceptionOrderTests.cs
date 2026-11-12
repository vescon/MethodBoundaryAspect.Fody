using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions;
using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// An async method throws before its first await: everything runs on the calling thread, in a fixed order.
    /// </summary>
    public class AsyncExceptionOrderTests
    {
        [Fact]
        public async Task EntryExceptionAndExitShouldRunBeforeTheTaskIsReturned()
        {
            var calls = new List<string>();
            var target = new AsyncExceptionOrderMethods(calls);

            var task = target.FailBeforeAwait();

            // OnExit runs when the method returns its (faulted) task, see the README: it is not skipped for a failed task
            calls.Should().Equal("OnEntry", "OnException InvalidOperationException ReturnValue=null", "OnExit");
            await task.Awaiting(t => t).Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task SwallowedExceptionShouldCompleteTheTaskWithTheReturnValue()
        {
            var calls = new List<string>();
            var target = new AsyncExceptionOrderMethods(calls);

            var task = target.FailAndSwallow();

            (await task).Should().Be(-1);
            calls.Should().Equal("OnEntry", "OnException InvalidOperationException ReturnValue=null", "OnExit");
        }
    }
}
