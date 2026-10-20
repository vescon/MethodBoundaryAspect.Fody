using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// Generic aspects ([MyAspect&lt;int&gt;], C# 11) were woven with the constructor and methods of the open generic
    /// type, which threw an InvalidOperationException ("not fully instantiated") when the method was called.
    /// </summary>
    [Collection(nameof(GenericAspectLog))]
    public class GenericAspectTests
    {
        public GenericAspectTests()
        {
            GenericAspectLog.Calls.Clear();
        }

        [Fact]
        public void GenericAspectShouldBeCalledWithItsConstructorArgumentPropertyAndField()
        {
            new GenericAspectMethods().Sync(5).Should().Be(5);

            GenericAspectLog.Calls.Should().Equal(
                "A Int32 OnEntry Sync Label=label Number=3",
                "A Int32 OnExit Sync 5");
        }

        [Fact]
        public void GenericAspectShouldBeCalledOnException()
        {
            Action action = () => new GenericAspectMethods().Throwing();

            action.Should().Throw<InvalidOperationException>();
            GenericAspectLog.Calls.Should().Equal(
                "B List`1 OnEntry Throwing Label= Number=0",
                "B List`1 OnException Throwing InvalidOperationException");
        }

        [Fact]
        public async Task GenericAspectShouldBeCalledOnAsyncMethod()
        {
            (await new GenericAspectMethods().Async()).Should().Be(7);

            GenericAspectLog.Calls.Should().Contain("C Guid OnEntry Async Label= Number=0");
            GenericAspectLog.Calls.Should().Contain(c => c.StartsWith("C Guid OnExit Async"));
        }

        [Fact]
        public async Task GenericAspectShouldBeCalledOnExceptionOfAsyncMethod()
        {
            Func<Task> action = () => new GenericAspectMethods().AsyncThrowing();

            await action.Should().ThrowAsync<InvalidOperationException>();
            GenericAspectLog.Calls.Should().Contain("D Int32 OnException AsyncThrowing InvalidOperationException");
        }

        [Fact]
        public void NonGenericAspectDerivedFromGenericAspectShouldBeCalled()
        {
            new GenericAspectMethods().FromGenericBase().Should().Be(1);

            GenericAspectLog.Calls.Should().Equal("String OnEntry FromGenericBase");
        }

        [Fact]
        public void GenericAspectShouldBeCalledOnMethodOfGenericClass()
        {
            new GenericAspectOnGenericClass<string>().Method("value", 1).Should().Be("value");

            GenericAspectLog.Calls.Should().Equal(
                "E Byte OnEntry Method Label= Number=0",
                "E Byte OnExit Method value");
        }
    }
}
