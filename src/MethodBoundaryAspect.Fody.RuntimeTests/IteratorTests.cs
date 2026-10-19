using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// Iterator methods (also async ones) are woven like synchronous methods, their code runs in the compiler
    /// generated state machine while the result is enumerated, without the aspect (see README).
    /// </summary>
    [Collection(nameof(IteratorRecordingAspect))]
    public class IteratorTests
    {
        public IteratorTests()
        {
            IteratorRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void IteratorShouldCallTheAspectWhenTheEnumerableIsReturned()
        {
            var values = new IteratorMethods().ThrowWhileEnumerating();

            IteratorRecordingAspect.Calls.Should().Equal(
                "OnEntry ThrowWhileEnumerating",
                $"OnExit ThrowWhileEnumerating returned {values.GetType().Name}");

            Action enumerate = () => values.ToList();
            enumerate.Should().Throw<InvalidOperationException>().WithMessage("enumerating");
            IteratorRecordingAspect.Calls.Should().HaveCount(2, "OnException is not called while enumerating");
        }

#if NETCOREAPP
        [Fact]
        public async Task AsyncIteratorShouldCallTheAspectWhenTheEnumerableIsReturned()
        {
            var values = new IteratorMethods().ThrowWhileEnumeratingAsync();

            IteratorRecordingAspect.Calls.Should().Equal(
                "OnEntry ThrowWhileEnumeratingAsync",
                $"OnExit ThrowWhileEnumeratingAsync returned {values.GetType().Name}");

            Func<Task> enumerate = async () =>
            {
                await foreach (var _ in values)
                {
                }
            };
            await enumerate.Should().ThrowAsync<InvalidOperationException>().WithMessage("enumerating");
            IteratorRecordingAspect.Calls.Should().HaveCount(2, "OnException is not called while enumerating");
        }
#endif
    }
}
