using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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

        [Fact]
        public void IfTheSchemaIsRead_ThenItDescribesTheSettingsTheWeaverKnows()
        {
            var schemaPath = Path.Combine(AppContext.BaseDirectory, "MethodBoundaryAspect.Fody.xcf");
            var xs = XNamespace.Get("http://www.w3.org/2001/XMLSchema");
            var schema = XDocument.Load(schemaPath).Root;

            var schemaAttributes = schema.Elements(xs + "attribute").Select(a => (string)a.Attribute("name")).ToList();
            var schemaElements = schema.Descendants(xs + "choice").Elements(xs + "element").Select(e => (string)e.Attribute("name")).ToList();

            schemaAttributes.Should().BeEquivalentTo(GetKnownSettings("KnownAttributes"));
            schemaElements.Should().BeEquivalentTo(GetKnownSettings("KnownElements"));
        }

        [Fact]
        public void IfTheReadmeShowsAConfiguration_ThenItOnlyUsesKnownSettings()
        {
            var readme = File.ReadAllText(FindFile("README.md"));
            var examples = Regex.Matches(readme, @"<MethodBoundaryAspect(?: [^<>/]*)?(?:/>|>.*?</MethodBoundaryAspect>)", RegexOptions.Singleline)
                .Cast<Match>()
                .Select(m => XElement.Parse(m.Value))
                .ToList();
            examples.Should().NotBeEmpty();

            foreach (var example in examples)
            {
                Weave(example);
            }

            _warnings.Where(w => w.Contains("Unknown setting")).Should().BeEmpty();
        }

        private static string[] GetKnownSettings(string fieldName) =>
            (string[])typeof(ModuleWeaver).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        private static string FindFile(string name)
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                var path = Path.Combine(directory.FullName, name);
                if (File.Exists(path))
                    return path;
            }

            throw new FileNotFoundException(name);
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
