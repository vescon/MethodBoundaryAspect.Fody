#pragma warning disable 0618 // the obsolete aspect roles are the subject of these tests
using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;
using Mono.Cecil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// Aspect roles ([ProvideAspectRole], [AspectRoleDependency]) are obsolete and will be removed in version 4,
    /// [AspectOrderIndex] replaces them.
    /// </summary>
    public class ObsoleteRoleTests
    {
        [Theory]
        [InlineData("ProvideAspectRoleAttribute")]
        [InlineData("AspectRoleDependencyAttribute")]
        [InlineData("AspectDependencyAction")]
        [InlineData("AspectDependencyPosition")]
        public void IfATypeOfTheAspectRolesIsUsed_ThenTheCompilerWarns(string typeName)
        {
            // the runtime library is no direct reference of this project, get it via the base class of an aspect
            var runtimeLibrary = typeof(RoleOnlyAspect).BaseType.Assembly;

            var type = runtimeLibrary.GetType("MethodBoundaryAspect.Fody.Attributes." + typeName, throwOnError: true);

            type.GetCustomAttribute<ObsoleteAttribute>().Should().NotBeNull();
        }

        [Fact]
        public void IfAspectsUseRoles_ThenOneWarningIsWrittenPerAspect()
        {
            var warnings = Weave(typeof(ObsoleteRoleMethods));

            warnings.Should().HaveCount(2);
            warnings.Should().ContainSingle(w => w.Contains($"'{typeof(RoleOnlyAspect).FullName}'"));
            warnings.Should().ContainSingle(w => w.Contains($"'{typeof(RoleDependencyOnlyAspect).FullName}'"));
            warnings.Should().OnlyContain(w => w.Contains("[AspectOrderIndex]") && w.Contains("version 4"));
        }

        [Fact]
        public void IfNoAspectUsesRoles_ThenNoWarningIsWritten()
        {
            var warnings = Weave(typeof(RefStructWarningMethods));

            warnings.Should().NotContain(w => w.Contains("AspectRole"));
        }

        private static List<string> Weave(Type type)
        {
            var warnings = new List<string>();

            var weaver = new ModuleWeaver
            {
                ModuleDefinition = WeaverTestHelper.ReadModule(type)
            }.CaptureWarnings(warnings);
            weaver.AddClassFilter(type.FullName);
            weaver.Execute();
            return warnings;
        }
    }
}
