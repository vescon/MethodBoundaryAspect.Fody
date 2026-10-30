using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// A typo in FodyWeavers.xml (e.g. "SupressRefStructWarnings") must not be ignored silently.
    /// </summary>
    public class ConfigurationWarningTests
    {
        private readonly List<string> _warnings = new List<string>();

        [Fact]
        public void IfASettingIsUnknown_ThenAWarningIsWritten()
        {
            var config = new XElement("MethodBoundaryAspect",
                new XAttribute("SupressRefStructWarnings", "true"),
                new XElement("DisableAspectInstanceRuse", new XAttribute("Aspect", "My.Aspect")));

            Weave(config);

            _warnings.Should().ContainSingle(w => w.Contains("'SupressRefStructWarnings'"));
            _warnings.Should().ContainSingle(w => w.Contains("'DisableAspectInstanceRuse'"));
        }

        [Fact]
        public void IfAllSettingsAreKnown_ThenNoWarningAboutSettingsIsWritten()
        {
            var config = new XElement("MethodBoundaryAspect",
                new XAttribute("SuppressRefStructWarnings", "true"),
                new XAttribute("DisableExecutionArgsOptimization", "false"),
                new XAttribute("DisableAspectInstanceReuse", "false"),
                new XElement("DisableExecutionArgsOptimization", new XAttribute("Aspect", "My.Aspect")),
                new XElement("DisableAspectInstanceReuse", new XAttribute("Aspect", "My.Aspect")));

            Weave(config);

            _warnings.Where(w => w.Contains("Unknown setting")).Should().BeEmpty();
        }

        private void Weave(XElement config)
        {
            // Fody sets these (for consumers obsolete) callbacks, they are the only way to capture the warnings
#pragma warning disable CS0618
            var weaver = new ModuleWeaver
            {
                Config = config,
                ModuleDefinition = WeaverTestHelper.ReadModule(typeof(ObsoleteRoleMethods)),
                LogWarning = _warnings.Add,
                LogWarningPoint = (message, _) => _warnings.Add(message)
            };
#pragma warning restore CS0618
            weaver.AddClassFilter(typeof(ObsoleteRoleMethods).FullName);
            weaver.Execute();
        }
    }
}
