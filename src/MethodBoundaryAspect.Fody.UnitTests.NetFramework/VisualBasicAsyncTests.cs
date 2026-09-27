using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.VisualBasic;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    /// <summary>
    /// The Visual Basic compiler emits a different catch block in the async state machine than C#
    /// (ProjectData.SetProjectError/ClearProjectError around SetException),
    /// see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/119
    /// </summary>
    public class VisualBasicAsyncTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Fact]
        public void IfVisualBasicAsyncTaskMethodThrows_ThenOnExceptionIsCalled()
        {
            // Arrange
            var testClassType = typeof(VbAsyncClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), nameof(VbAsyncClass.AttemptThrowTask));

            // Assert
            result.Should().Be("[OnEntry][OnExit][OnException:VB exception][Caught:VB exception]");
        }

        [Fact]
        public void IfVisualBasicAsyncTaskOfTMethodThrows_ThenOnExceptionIsCalled()
        {
            // Arrange
            var testClassType = typeof(VbAsyncClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), nameof(VbAsyncClass.AttemptThrowTaskOfT));

            // Assert
            result.Should().Be("[OnEntry][OnExit][OnException:VB exception][Caught:VB exception]");
        }

        [Fact]
        public void IfVisualBasicAsyncTaskOfTMethodReturns_ThenValueIsReturned()
        {
            // Arrange
            var testClassType = typeof(VbAsyncClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), nameof(VbAsyncClass.AttemptReturnValue));

            // Assert
            result.Should().Be("[OnEntry][OnExit][Value:7]");
        }

        [Fact]
        public void IfVisualBasicAsyncTaskOfTMethodThrowsAndAspectSwallowsException_ThenAspectReturnValueIsReturned()
        {
            // Arrange
            var testClassType = typeof(VbAsyncClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), nameof(VbAsyncClass.AttemptSwallowException));

            // Assert
            result.Should().Be("[OnException:VB exception][Value:42]");
        }

        [Fact]
        public void IfVisualBasicSyncMethodThrows_ThenOnExceptionIsCalled()
        {
            // Arrange
            var testClassType = typeof(VbAsyncClass);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), nameof(VbAsyncClass.AttemptThrowSync));

            // Assert
            result.Should().Be("[OnEntry][OnException:VB exception][Caught:VB exception]");
        }
    }
}
