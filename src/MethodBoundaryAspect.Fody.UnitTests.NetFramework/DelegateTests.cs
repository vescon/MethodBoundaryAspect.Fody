using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class DelegateTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Fact]
        public void IfClassWithDelegateIsWeaved_ThenThereShouldBeNoException()
        {
            // Arrange
            AllowNothingWeaved = true; // the methods of a delegate have no body
            var type = typeof(ClassWithDelegate);

            // Act
            Action action = () => WeaveAssemblyClassWithNestedTypesAndLoad(type);

            // Assert

            action.Should().NotThrow();
        }
    }
}