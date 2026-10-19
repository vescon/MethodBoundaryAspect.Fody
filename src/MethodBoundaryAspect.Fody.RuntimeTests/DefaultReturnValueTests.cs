using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// If the method body is skipped or an exception is swallowed and no aspect sets the ReturnValue, the method
    /// returns the default value:
    /// - a ReturnValue of null was unboxed to a value type (NullReferenceException) if any aspect of the method
    ///   writes the ReturnValue somewhere, so it is read back,
    /// - async methods always read it back when an exception is swallowed: the task completed with a
    ///   NullReferenceException, or an InvalidCastException if the ReturnValue still held the task returned to the caller.
    /// </summary>
    public class DefaultReturnValueTests
    {
        [Fact]
        public void IfAnAspectSetsTheReturnValue_ThenItIsReturned()
        {
            new DefaultReturnValueMethods().CachedInt().Should().Be(99);
        }

        [Fact]
        public void IfAnExceptionIsSwallowedWithoutReturnValue_ThenTheDefaultValueIsReturned()
        {
            new DefaultReturnValueMethods().SwallowedInt().Should().Be(0);
        }

        [Fact]
        public void IfAnExceptionIsSwallowedWithoutReturnValueInAGenericMethod_ThenTheDefaultValueIsReturned()
        {
            var target = new DefaultReturnValueMethods();

            target.SwallowedGeneric<int>().Should().Be(0);
            target.SwallowedGeneric<string>().Should().BeNull();
        }

        [Fact]
        public async Task IfAnAsyncExceptionIsSwallowedAfterAwaitWithoutReturnValue_ThenTheDefaultValueIsTheResult()
        {
            (await new DefaultReturnValueMethods().SwallowedAfterAwaitAsync()).Should().Be(0);
        }

        [Fact]
        public async Task IfAnAsyncExceptionIsSwallowedBeforeAwaitWithoutReturnValue_ThenTheDefaultValueIsTheResult()
        {
            (await new DefaultReturnValueMethods().SwallowedBeforeAwaitAsync()).Should().Be(0);
        }

        [Fact]
        public async Task IfTheReturnedTaskIsInTheReturnValue_ThenItIsNotTheResult()
        {
            var target = new DefaultReturnValueMethods();

            (await target.SwallowedAfterAwaitWithReturnValueReadAsync()).Should().BeNull();
            (await target.SwallowedIntAfterAwaitWithReturnValueReadAsync()).Should().Be(0);
        }

        [Fact]
        public async Task IfAnAsyncExceptionIsSwallowedWithReturnValue_ThenItIsTheResult()
        {
            (await new DefaultReturnValueMethods().SwallowedWithReturnValueAsync()).Should().Be(42);
        }
    }
}
