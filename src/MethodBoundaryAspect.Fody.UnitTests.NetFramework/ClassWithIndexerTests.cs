using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class ClassWithIndexerTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Fact]
        public void IfClassContainsIndexer_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DummyMethod";
            var testClassType = typeof(ClassWithIndexer);
            
            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), testMethodName);

            // Assert
            RunIlSpy();
            call.Should().NotThrow();
        }
    }
}