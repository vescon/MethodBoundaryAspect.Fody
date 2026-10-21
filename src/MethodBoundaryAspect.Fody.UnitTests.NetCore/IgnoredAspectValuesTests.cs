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
    /// The weaver doesn't run the code of aspects, so a filter set in a constructor or by overriding the property
    /// was ignored without a hint. CompileTimeValidate is never called either.
    /// </summary>
    public class IgnoredAspectValuesTests
    {
        [Fact]
        public void IfAnAspectSetsValuesInItsCode_ThenOneWarningNamesThemPerAspect()
        {
            var warnings = Weave(typeof(IgnoredValueMethods));

            warnings.Should().HaveCount(3);
            warnings.Should().ContainSingle(w => w.Contains($"'{typeof(ConstructorFilterAspect).FullName}' sets AttributeTargetMemberAttributes, NamespaceFilter in its code"));
            warnings.Should().ContainSingle(w => w.Contains($"'{typeof(DerivedConstructorFilterAspect).FullName}' sets AttributeTargetMemberAttributes, NamespaceFilter in its code"));
            warnings.Should().ContainSingle(w => w.Contains($"'{typeof(OverridingFilterAspect).FullName}' sets TypeNameFilter in its code"));
        }

        [Fact]
        public void IfAnAspectSetsValuesInItsCode_ThenTheWarningSaysWhereToSetThem()
        {
            var warnings = Weave(typeof(IgnoredValueMethods));

            warnings.Should().OnlyContain(w =>
                w.Contains("The weaver does not run the code of aspects") && w.Contains("Set it where the aspect is applied"));
            warnings.Should().ContainSingle(w => w.Contains("[OverridingFilterAspect(TypeNameFilter = ...)]"));
        }

        [Fact]
        public void IfValuesAreSetWhereTheAspectIsApplied_ThenNoWarningIsWritten()
        {
            var warnings = Weave(typeof(RefStructWarningMethods));

            warnings.Should().NotContain(w => w.Contains("in its code"));
            Weave(typeof(IgnoredValueMethods)).Should().NotContain(w => w.Contains(typeof(OnlyOnEntryAspect).FullName));
        }

        [Fact]
        public void IfCompileTimeValidateIsUsed_ThenTheCompilerWarns()
        {
            // the runtime library is no direct reference of this project, get it via the base class of an aspect
            var runtimeLibrary = typeof(ConstructorFilterAspect).BaseType.Assembly;
            var aspect = runtimeLibrary.GetType("MethodBoundaryAspect.Fody.Attributes.OnMethodBoundaryAspect", throwOnError: true);

            var method = aspect.GetMethod("CompileTimeValidate");

            method.GetCustomAttribute<ObsoleteAttribute>().Should().NotBeNull();
            method.GetCustomAttribute<ObsoleteAttribute>().Message.Should().Contain("never called").And.Contain("version 4");
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
