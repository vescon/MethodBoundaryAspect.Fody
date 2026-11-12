using AwesomeAssertions;
using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Threading;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Does nothing but counting its calls. The tests which only check that a woven method runs without an exception
    /// use <see cref="ExpectCalls"/>, otherwise they also pass if the method is not woven at all. The tests which
    /// use the aspect are in one collection, <see cref="CollectionName"/>, so that the counters are not changed
    /// concurrently.
    /// </summary>
    public class EmptyMethodBoundaryAspect : OnMethodBoundaryAspect
    {
        public const string CollectionName = "EmptyMethodBoundaryAspect";

        private static int _entries;
        private static int _exits;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Interlocked.Increment(ref _entries);
        }

        public override void OnException(MethodExecutionArgs arg)
        {
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            Interlocked.Increment(ref _exits);
        }

        /// <summary>Asserts when it is disposed that OnEntry and OnExit have been called since it was created.</summary>
        public static IDisposable ExpectCalls() => new CallScope();

        private sealed class CallScope : IDisposable
        {
            private readonly int _entriesBefore = Volatile.Read(ref _entries);
            private readonly int _exitsBefore = Volatile.Read(ref _exits);

            public void Dispose()
            {
                Volatile.Read(ref _entries).Should().BeGreaterThan(_entriesBefore, "the method has to be woven, otherwise the test proves nothing");
                Volatile.Read(ref _exits).Should().BeGreaterThan(_exitsBefore, "the method has to be woven, otherwise the test proves nothing");
            }
        }
    }
}
