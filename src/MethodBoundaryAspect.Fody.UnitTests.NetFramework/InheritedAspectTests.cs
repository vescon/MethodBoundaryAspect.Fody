using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class InheritedAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private static readonly Type TestClassType = typeof(InheritedAspectMethods);

        [Fact]
        public void IfAspectDerivesFromAbstractAspectBase_ThenStaticMethodShouldBeWeaved()
        {
            // Arrange
            const string testMethodName = nameof(InheritedAspectMethods.StaticMethodCall);
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("[InheritedAspect_OnEntry][InheritedAspect_OnExit:]");
        }

        [Fact]
        public void IfAspectDerivesFromAbstractAspectBase_ThenInstanceMethodShouldBeWeaved()
        {
            // Arrange
            const string testMethodName = nameof(InheritedAspectMethods.InstanceMethodCall);
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("[InheritedAspect_OnEntry][InheritedAspect_OnExit:]");
        }

        [Fact]
        public void IfAspectDerivesFromAbstractAspectBase_ThenReturnValueShouldBePassedToAspect()
        {
            // Arrange
            const string testMethodName = nameof(InheritedAspectMethods.MethodWithReturnValue);
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName, 21);

            // Assert
            result.Should().Be("[InheritedAspect_OnEntry][InheritedAspect_OnExit:42]");
        }

        [Fact]
        public void IfAspectHooksAreOverriddenOnDifferentInheritanceLevels_ThenAllHooksShouldBeCalled()
        {
            // Arrange
            const string testMethodName = nameof(InheritedAspectMethods.MethodWithAspectHooksOnDifferentInheritanceLevels);
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            // Assert
            result.Should().Be("[InheritedOnEntryAspectBase_OnEntry][InheritedOnExitAspect_OnExit]");
        }
    }
}
