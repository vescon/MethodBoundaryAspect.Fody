using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class TypeAsObjectParameterTests : MethodBoundaryAspectTestBase
    {
        static readonly Type TestClassType = typeof(TypeAsObjectParameterClass);

        [Fact]
        public void IfTypeOfIsUsedInAttributeParameter_ThenTypeIsPassedCorrectly()
        {
            // Arrange
            const string testMethodName = "GetTypeName";
            WeaveAssemblyClassAndLoad(TestClassType);

            // Act
            var result = (string[])AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName, "System.Int32");

            // Assert
            result[0].Should().Be(TestClassType.FullName);
        }

        [Fact]
        public void IfAttributeHasMultipleConstructorsOfSameLength_ThenCorrectConstructorIsUsed()
        {
            // Arrange
            const string testMethodName = "GetTypeName3";
            WeaveAssemblyClassAndLoad(TestClassType);

            // Act
            var result = (string[])AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result[0].Should().Be("ExpectedResult");
        }
    }
}