using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;
using Mono.Cecil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// The aspects have no [AttributeUsage], so the compiler accepts them on a constructor, a field, a parameter,
    /// the return value, an event or the module. The weaver never reads them there and warns.
    /// </summary>
    public class IgnoredAspectTargetsTests
    {
        private readonly List<string> _warnings = new List<string>();
        private readonly ModuleWeaver _weaver;
        private readonly ModuleDefinition _module;

        public IgnoredAspectTargetsTests()
        {
            _module = WeaverTestHelper.ReadModule(typeof(IgnoredAspectTargets));
            _weaver = new ModuleWeaver { ModuleDefinition = _module }.CaptureWarnings(_warnings);
        }

        [Theory]
        [InlineData("the constructor")]
        [InlineData("the field")]
        [InlineData("the event")]
        [InlineData("the return value of")]
        [InlineData("the parameter 'parameter'")]
        public void IfAnAspectIsAppliedWhereItIsIgnored_ThenAWarningIsWritten(string target)
        {
            _weaver.AddClassFilter(typeof(IgnoredAspectTargets).FullName);

            _weaver.Execute();

            _warnings.Should().ContainSingle(w => w.Contains(target))
                .Which.Should().Contain(typeof(OnlyOnEntryAspect).FullName)
                .And.Contain("is ignored");
        }

        [Fact]
        public void IfAnAspectIsAppliedToAProperty_ThenNoWarningIsWritten()
        {
            _weaver.AddClassFilter(typeof(IgnoredAspectTargets).FullName);

            _weaver.Execute();

            _warnings.Should().HaveCount(5).And.NotContain(w => w.Contains("Property"));
            _weaver.TotalWeavedProperties.Should().Be(2);
        }

        [Theory]
        [InlineData(typeof(IIgnoredAspectTargets), "the interface method", "InterfaceMethod", "not inherited")]
        [InlineData(typeof(IIgnoredAspectTargets), "the interface property", "InterfaceProperty", "not inherited")]
        [InlineData(typeof(IgnoredAspectTargetsWithoutBody), "the abstract method", "AbstractMethod", "not inherited")]
        [InlineData(typeof(IgnoredAspectTargetsWithoutBody), "the abstract property", "AbstractProperty", "not inherited")]
        [InlineData(typeof(IgnoredAspectTargetsWithoutBody), "the extern method", "GetCurrentThreadId", "no body")]
        public void IfAnAspectIsAppliedToAMethodWithoutBody_ThenAWarningIsWritten(System.Type type, string target, string name, string hint)
        {
            _weaver.AddClassFilter(type.FullName);

            _weaver.Execute();

            _warnings.Should().ContainSingle(w => w.Contains(target))
                .Which.Should().Contain(name)
                .And.Contain("is ignored")
                .And.Contain(hint);
        }

        [Fact]
        public void IfAnAspectIsAppliedToAMethodWithBodyOfAnAbstractClass_ThenItIsWovenWithoutWarning()
        {
            _weaver.AddClassFilter(typeof(IgnoredAspectTargetsWithoutBody).FullName);

            _weaver.Execute();

            _warnings.Should().HaveCount(3).And.NotContain(w => w.Contains("MethodWithBody"));
            _weaver.TotalWeavedMethods.Should().Be(1);
        }

        [Fact]
        public void IfAnAspectIsAppliedToTheModule_ThenAWarningIsWritten()
        {
            var aspect = _module.GetType(typeof(IgnoredAspectTargets).FullName).Fields.Single(f => f.Name == "_field").CustomAttributes.Single();
            _module.CustomAttributes.Add(new CustomAttribute(aspect.Constructor));

            _weaver.Execute();

            _warnings.Should().Contain(w => w.Contains("applied to the module"));
        }
    }
}
