using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class GenericClassWithOpenTypeTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Fact]
        public void IfCGenericClassWithOpenType_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = nameof(GenericClassWithOpenType.CallOpenTypeMethod);
            var testClassType = typeof(GenericClassWithOpenType);

            // Act
            WeaveAssemblyMethodAndLoad(testClassType, nameof(GenericClassWithOpenType.OpenTypeMethod));
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName, 42);

            // Assert
            result.Should().Be("OpenTypeMethod");
        }
    }
}