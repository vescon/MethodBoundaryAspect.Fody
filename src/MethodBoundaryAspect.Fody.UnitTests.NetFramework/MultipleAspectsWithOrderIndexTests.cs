using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.NetFramework.MultipleAspects;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class MultipleAspectsWithOrderIndexTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private static readonly Type TestClassType = typeof(MultipleAspectsWithOrderIndexMethods);

        [Fact]
        public void IMethodWithOrderIndexSpecifiedOnClassLevelIsCalled_ThenTheOnMethodBoundaryAspectsShouldBeCalledInCorrectOrder()
        {
            // Arrange
            const string testMethodName = "MethodWithOrderIndexSpecifiedOnClassLevel";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("[Aspect3_OnEntry][Aspect2_OnEntry][Aspect1_OnEntry][Aspect1_OnExit][Aspect2_OnExit][Aspect3_OnExit]");
        }

        [Fact]
        public void IfMethodWithOrderIndexSpecifiedIsCalled_ThenTheOnMethodBoundaryAspectsShouldBeCalledInCorrectOrder()
        {
            // Arrange
            const string testMethodName = "MethodWithOrderIndexSpecified";
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("[Aspect2_OnEntry][Aspect1_OnEntry][Aspect3_OnEntry][Aspect3_OnExit][Aspect1_OnExit][Aspect2_OnExit]");
        }
    }
}