using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    /// <summary>
    /// An aspect restricted to a visibility (AttributeTargetMemberAttributes) only weaves the methods of that
    /// visibility: the public method calls the others in a chain, the aspect replaces the result of its methods.
    /// </summary>
    public class MethodVisibilityAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Theory]
        [InlineData(typeof(PublicMethodAttributeAspectMethods), "PublicMethod")]
        [InlineData(typeof(ProtectedInternalMethodAttributeAspectMethods), "ProtectedInternalMethod")]
        [InlineData(typeof(PrivateProtectedMethodAttributeAspectMethods), "PrivateProtectedMethod")]
        [InlineData(typeof(InternalMethodAttributeAspectMethods), "InternalMethod")]
        [InlineData(typeof(ProtectedMethodAttributeAspectMethods), "ProtectedMethod")]
        [InlineData(typeof(PrivateMethodAttributeAspectMethods), "PrivateMethod")]
        public void IfMethodIsCalled_ThenTheOnMethodBoundaryAspectShouldBeCalledForTheMethodsOfItsVisibility(
            Type testClassType, string expectedResult)
        {
            // Arrange
            WeaveAssemblyClassAndLoad(testClassType);

            // Act
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), "PublicMethod");

            // Assert
            result.Should().Be(expectedResult);
        }
    }
}
