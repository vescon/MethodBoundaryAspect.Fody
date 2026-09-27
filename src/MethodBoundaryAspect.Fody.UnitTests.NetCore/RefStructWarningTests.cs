using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// Methods using ref structs (e.g. Span&lt;T&gt;) are weaved with null values for them and a warning,
    /// see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/129
    /// </summary>
    [Collection(TestAssemblyNetCoreWeaving)]
    public class RefStructWarningTests : IDisposable
    {
        // shadow copies of the same assembly are named by Environment.TickCount, so weave them sequentially
        public const string TestAssemblyNetCoreWeaving = "TestAssembly.NetCore weaving";

        private readonly List<string> _weavedAssemblyPaths = new List<string>();

        [Fact]
        public void IfMethodsUsingRefStructsAreWeaved_ThenAWarningWithSuppressionHintIsWrittenPerMethod()
        {
            var warnings = Weave(config: null);

            warnings.Should().HaveCount(2);
            warnings.Should().ContainSingle(w => w.Contains("RefStructWarningMethods::SpanParameter(") && w.Contains("(parameter 'value')"));
            warnings.Should().ContainSingle(w => w.Contains("RefStructWarningMethods::SpanReturn(") && w.Contains("(return value)"));
            warnings.Should().OnlyContain(w =>
                w.Contains("SuppressRefStructWarnings=\"true\"") && w.Contains("FodyWeavers.xml") && w.Contains("[DisableWeaving]"));
        }

        [Fact]
        public void IfRefStructWarningsAreSuppressedInConfig_ThenNoWarningIsWritten()
        {
            var warnings = Weave(new XElement("MethodBoundaryAspect", new XAttribute("SuppressRefStructWarnings", "true")));

            warnings.Should().BeEmpty();
        }

        [Fact]
        public void IfRefStructWarningsAreNotSuppressedInConfig_ThenWarningsAreWritten()
        {
            var warnings = Weave(new XElement("MethodBoundaryAspect", new XAttribute("SuppressRefStructWarnings", "false")));

            warnings.Should().HaveCount(2);
        }

        private List<string> Weave(XElement config)
        {
            var warnings = new List<string>();
            // Fody sets these (for consumers obsolete) callbacks, they are the only way to capture the warnings
#pragma warning disable CS0618
            var weaver = new ModuleWeaver
            {
                Config = config,
                LogWarning = warnings.Add,
                LogWarningPoint = (message, _) => warnings.Add(message)
            };
#pragma warning restore CS0618
            weaver.AddClassFilter(typeof(RefStructWarningMethods).FullName);

            var assemblyPath = typeof(RefStructWarningMethods).Assembly.Location;
            _weavedAssemblyPaths.Add(weaver.WeaveToShadowFile(assemblyPath, ModuleHelper.AssemblyResolver));

            weaver.TotalWeavedMethods.Should().Be(3, "the ref struct methods are still weaved, only [DisableWeaving] is skipped");
            return warnings;
        }

        public void Dispose()
        {
            foreach (var path in _weavedAssemblyPaths)
            {
                File.Delete(path);
                File.Delete(Path.ChangeExtension(path, "pdb"));
            }
        }
    }
}
