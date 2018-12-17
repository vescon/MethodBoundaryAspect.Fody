using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using Xunit;


namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class DelegateTests : MethodBoundaryAspectTestBase
    {
        [Fact]
        public void IfClassWithDelegateIsWeaved_ThenThereShouldBeNoException()
        {
            // Arrange
            var type = typeof(ClassWithDelegate);

            // Act
            Action action = () => WeaveAssemblyClassWithNestedTypesAndLoad(type);

            // Assert

            action.Should().NotThrow();
        }
    }
}