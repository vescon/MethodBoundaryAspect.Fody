using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class ClassWithIndexerTests : MethodBoundaryAspectTestBase
    {
        [Fact]
        public void IfClassContainsIndexer_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            const string testMethodName = "DummyMethod";
            var testClassType = typeof(ClassWithIndexer);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            Action call = () => AssemblyLoader.InvokeMethod(testClassType.FullName, testMethodName);

            // Assert
            RunIlSpy();
            call.ShouldNotThrow();
        }
    }
}