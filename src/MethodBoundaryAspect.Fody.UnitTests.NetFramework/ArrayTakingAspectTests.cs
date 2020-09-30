using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class ArrayTakingAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        static readonly Type TestClassType = typeof(ArrayTakingAspectMethod);

        [Fact]
        public void IfAttributeConstructorTakesArray_ThenArrayIsUsed()
        {
            // Arrange
            const string testMethodName = "Method";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            int[] args = result.Should().BeOfType<int[]>().Subject;
            args.Length.Should().Be(2);
            args[0].Should().Be(0);
            args[1].Should().Be(42);
        }
    }
}