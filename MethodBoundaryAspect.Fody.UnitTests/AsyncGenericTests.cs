using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class AsyncGenericTests : MethodBoundaryAspectTestBase
    {
        Type OpenClass = typeof(AsyncClass<>);
        TypeInfo Closed = typeof(AsyncClass<>).TypeInfoWithGenericParameters(typeof(Placeholder));

        [Fact]
        public void IfGenericClassIsWeavedForOnException_ThenExceptionThrownBetweenAwaitsIsHandled()
        {
            // Act
            WeaveAssemblyClassAndLoad(OpenClass);
            string result = AssemblyLoader.InvokeMethod(Closed, "AttemptTClass") as string;

            // Assert
            result.Should().Contain("OnException first");
        }

        [Fact]
        public void IfGenericClassIsWeavedForOnExit_ThenTaskIsPasedToAspectReturnValue()
        {
            // Act
            WeaveAssemblyClassAndLoad(OpenClass);
            object result = AssemblyLoader.InvokeMethod(Closed, "ReturnTClass");

            // Assert
            result.Should().Be(40);
        }

        [Fact]
        public void IfGenericClassIsWeavedForOnExit_ThenNonGenericTasksCanBeReturned()
        {
            // Act
            WeaveAssemblyClassAndLoad(OpenClass);
            object result = AssemblyLoader.InvokeMethod(Closed, "TestReturnInt");

            // Assert
            result.Should().Be(50);
        }
    }
}
