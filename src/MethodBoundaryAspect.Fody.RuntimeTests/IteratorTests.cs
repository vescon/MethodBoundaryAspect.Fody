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

        [Fact]
        public void EnumeratingAnIteratorSeveralTimesShouldNotCallTheAspectAgain()
        {
            var values = new IteratorMethods().Numbers();

            values.ToList().Should().Equal(1, 2);
            values.ToList().Should().Equal(1, 2);

            // the state machine of the iterator is not woven: no call of MoveNext, Dispose or GetEnumerator
            IteratorRecordingAspect.Calls.Should().HaveCount(2);
        }

#if NETCOREAPP
        [Fact]
        public async Task EnumeratingAnAsyncIteratorShouldNotCallTheAspectAgain()
        {
            var values = new IteratorMethods().NumbersAsync();

            var result = new System.Collections.Generic.List<int>();
            await foreach (var value in values)
                result.Add(value);

            result.Should().Equal(1, 2);
            IteratorRecordingAspect.Calls.Should().Equal(
                "OnEntry NumbersAsync",
                $"OnExit NumbersAsync returned {values.GetType().Name}");
        }

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
