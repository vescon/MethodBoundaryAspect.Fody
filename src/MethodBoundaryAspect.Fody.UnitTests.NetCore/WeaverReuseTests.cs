using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// One ModuleWeaver instance weaving two modules: the state of the first run (warnings written once per aspect,
    /// aspects of the configuration) must not change the second run.
    /// </summary>
    public class WeaverReuseTests
    {
        private readonly List<string> _warnings = new List<string>();

        [Fact]
        public void IfTheWeaverRunsTwice_ThenEachRunWarnsAboutTheObsoleteRoles()
        {
            var weaver = CreateWeaver(null, typeof(ObsoleteRoleMethods));

            weaver.Execute();
            var warningsOfFirstRun = _warnings.Count;
            weaver.ModuleDefinition = ReadModule();
            weaver.Execute();

            warningsOfFirstRun.Should().Be(2);
            _warnings.Should().HaveCount(2 * warningsOfFirstRun);
        }

        [Fact]
        public void IfTheWeaverRunsTwice_ThenTheConfiguredAspectsAreNotAddedTwice()
        {
            var config = new XElement("MethodBoundaryAspect",
                new XElement("DisableExecutionArgsOptimization", new XAttribute("Aspect", "Unknown.Aspect")),
                new XElement("DisableAspectInstanceReuse", new XAttribute("Aspect", "Unknown.Aspect")));
            var weaver = CreateWeaver(config, typeof(ObsoleteRoleMethods));

            weaver.Execute();
            weaver.ModuleDefinition = ReadModule();
            weaver.Execute();

            weaver.DisableExecutionArgsOptimizationAspects.Should().ContainSingle();
            weaver.DisableAspectInstanceReuseAspects.Should().ContainSingle();
            _warnings.Count(w => w.Contains("'Unknown.Aspect'")).Should().Be(4); // two settings, two runs
        }

        private ModuleWeaver CreateWeaver(XElement config, Type type)
        {
            // Fody sets these (for consumers obsolete) callbacks, they are the only way to capture the warnings
#pragma warning disable CS0618
            var weaver = new ModuleWeaver
            {
                Config = config,
                ModuleDefinition = ReadModule(),
                LogWarning = _warnings.Add,
                LogWarningPoint = (message, _) => _warnings.Add(message)
            };
#pragma warning restore CS0618
            weaver.AddClassFilter(type.FullName);
            return weaver;
        }

        private static ModuleDefinition ReadModule() =>
            WeaverTestHelper.ReadModule(typeof(ObsoleteRoleMethods));
    }
}
