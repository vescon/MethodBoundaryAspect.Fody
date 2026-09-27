using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    /// <summary>
    /// Tests for https://github.com/vescon/MethodBoundaryAspect.Fody/issues/111
    /// </summary>
    public class AsyncVoidTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private static readonly System.Type TestClassType = typeof(AsyncVoidClass);

        [Fact]
        public void IfAsyncVoidMethodIsWeavedWithOnExceptionOnly_ThenExceptionIsPassedToAspectAndRethrown()
        {
            // Act
            WeaveAssemblyMethodAndLoad(TestClassType, nameof(AsyncVoidClass.ThrowWithExceptionOnlyAspect));
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), nameof(AsyncVoidClass.AttemptThrowWithExceptionOnlyAspect));

            // Assert
            result.Should().Be("[OnException ThrowWithExceptionOnlyAspect: An exception][Unhandled: An exception]");
        }

        [Fact]
        public void IfAsyncVoidMethodIsWeavedWithOnExceptionSettingFlowBehaviorContinue_ThenExceptionIsSwallowed()
        {
            // Act
            WeaveAssemblyMethodAndLoad(TestClassType, nameof(AsyncVoidClass.ThrowAndSwallow));
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), nameof(AsyncVoidClass.AttemptThrowAndSwallow));

            // Assert
            result.Should().Be("[OnException ThrowAndSwallow: An exception]");
        }

        [Fact]
        public void IfAsyncVoidMethodIsWeavedWithAllAspectMethods_ThenExceptionIsPassedToAspectAndRethrown()
        {
            // Act
            WeaveAssemblyMethodAndLoad(TestClassType, nameof(AsyncVoidClass.ThrowWithFullAspect));
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), nameof(AsyncVoidClass.AttemptThrowWithFullAspect));

            // Assert
            result.Should().Be("[OnEntry][OnExit][OnException: An exception][Unhandled: An exception]");
        }

        [Fact]
        public void IfAsyncVoidMethodIsWeavedWithAllAspectMethods_ThenOnEntryAndOnExitAreCalled()
        {
            // Act
            WeaveAssemblyMethodAndLoad(TestClassType, nameof(AsyncVoidClass.ReturnWithFullAspect));
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), nameof(AsyncVoidClass.AttemptReturnWithFullAspect));

            // Assert
            result.Should().Be("[OnEntry][OnExit]");
        }
    }
}
