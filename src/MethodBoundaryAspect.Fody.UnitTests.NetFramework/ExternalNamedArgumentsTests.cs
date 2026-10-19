using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    /// <summary>
    /// A field of an aspect from another assembly, set as named argument, was not imported
    /// into the woven module, and a field of a base class was not found at all.
    /// </summary>
    public class ExternalNamedArgumentsTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private static readonly Type TestClassType = typeof(ExternalNamedArgumentsMethods);

        [Fact]
        public void FieldOfAspectFromOtherAssemblyShouldBeSet()
        {
            const string testMethodName = nameof(ExternalNamedArgumentsMethods.ExternalAspect);
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            result.Should().Be("Field: external, Tags: a, b");
        }

        [Fact]
        public void FieldOfBaseClassFromOtherAssemblyShouldBeSet()
        {
            const string testMethodName = nameof(ExternalNamedArgumentsMethods.DerivedFromExternalAspect);
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            var result = AssemblyLoader.InvokeMethod(TestClassType.TypeInfo(), testMethodName);

            result.Should().Be("Field: derived, Tags: null");
        }
    }
}
