using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class OverwriteReturnTests : MethodBoundaryAspectTestBase
    {
        static readonly Type TestClassType = typeof(OverwriteReturnValues);

        public OverwriteReturnTests()
        {
            WeaveAssemblyClassAndLoad(TestClassType);
        }

        [Theory]
        [InlineData("TestWrongType")]
        [InlineData("ExternTestWrongType")]
        public void IfWrongtypeIsUsedForReturnValueOverwrite_ThenInvalidCastExceptionIsThrown(string testMethodName)
        {
            // Act
            Action invoke = () => AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            var exceptions = invoke.Should().Throw<TargetInvocationException>().Subject;
            var exception = exceptions.Single();
            exception.InnerException.Should().BeOfType<InvalidCastException>();
        }

        [Theory]
        [InlineData("TestReturnEnum")]
        [InlineData("ExternTestReturnEnum")]
        [InlineData("TestTry")]
        [InlineData("ExternTestTry")]
        [InlineData("TestCatch")]
        [InlineData("ExternTestCatch")]
        [InlineData("TestReturnString")]
        [InlineData("ExternTestReturnString")]
        [InlineData("TestGenericReturnOverwriteString")]
        [InlineData("ExternTestGenericReturnOverwriteString")]
        public void IfReturnValueIsOverwrittenFromCatchBlock_ThenOverwrittenValueIsReturned(string testMethodName)
        {
            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("Overwritten");
        }

        [Theory]
        [InlineData("TestCovariantGeneric")]
        [InlineData("ExternTestCovariantGeneric")]
        public void IfReturnValueIsOverwrittenCovariantGeneric_ThenOverwrittenValueIsReturned(string testMethodName)
        {
            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("System.String[]");
        }
        
        [Theory]
        [InlineData("TestGenericReturnOverwriteEnum")]
        [InlineData("ExternTestGenericReturnOverwriteEnum")]
        public void IfReturnValueIsOverwrittenInGenericReturningForeignEnum_ThenOverwrittenValueIsReturned(string testMethodName)
        {
            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("LongFifth");
        }

        [Theory]
        [InlineData("TestReturnInt")]
        [InlineData("ExternTestReturnInt")]
        [InlineData("TestGenericReturnOverwriteInt")]
        [InlineData("ExternTestGenericReturnOverwriteInt")]
        [InlineData("TestCustomStruct")]
        [InlineData("ExternTestCustomStruct")]
        [InlineData("TestGenericReturnOverwriteStruct")]
        [InlineData("ExternTestGenericReturnOverwriteStruct")]
        [InlineData("TestMultipleSimultaneousAspects")]
        public void IfReturnValueIsCustomStruct_ThenOverwrittenValueIsReturned(string testMethodName)
        {
            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("42");
        }
    }
}