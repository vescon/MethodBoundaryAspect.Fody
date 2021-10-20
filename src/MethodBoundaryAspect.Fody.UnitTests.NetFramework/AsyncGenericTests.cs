using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class AsyncGenericTests : MethodBoundaryAspectNetFrameworkTestBase
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
        public void IfGenericClassIsWeavedForOnExit_ThenTaskIsPassedToAspectReturnValue()
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

        [Fact]
        public void IfAsyncGenericMethodIsWeavedForOnExceptionButNotForOnEntry_ThenExceptionThrownBetweenAwaitsIsHandled()
        {
            // Act
            WeaveAssemblyClassAndLoad(OpenClass);
            string result = AssemblyLoader.InvokeMethod(Closed, "AttemptThrowWithoutEntry") as string;

            // Assert
            result.Should().Contain("OnException third");
        }
    }
}
