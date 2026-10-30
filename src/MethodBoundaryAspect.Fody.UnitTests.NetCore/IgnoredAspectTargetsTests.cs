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
