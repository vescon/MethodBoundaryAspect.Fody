using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Mono.Cecil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// An extension block (C# 14) is compiled to static methods of the class and to a nested grouping type with a copy
    /// of each member. The aspects of an extension property are read from the grouping type.
    /// </summary>
    public class ExtensionBlockTests
    {
        private readonly List<string> _warnings = new List<string>();
        private readonly ModuleWeaver _weaver;
        private readonly ModuleDefinition _module;

        public ExtensionBlockTests()
        {
            _module = WeaverTestHelper.ReadModule(typeof(ExtensionBlockTargets));
            _weaver = new ModuleWeaver { ModuleDefinition = _module }.CaptureWarnings(_warnings);
        }

        [Fact]
        public void IfAnAspectIsAppliedToAnExtensionProperty_ThenItsAccessorsAreWoven()
        {
            _weaver.AddClassFilter(typeof(ExtensionBlockTargets).FullName);

            _weaver.Execute();

            var executors = _module.GetType(typeof(ExtensionBlockTargets).FullName).Methods
                .Where(m => m.Name.StartsWith("$_executor_"))
                .Select(m => m.Name + "(" + string.Join(",", m.Parameters.Select(p => p.ParameterType.Name)) + ")");
            executors.Should().BeEquivalentTo(
                "$_executor_get_Size(String)",
                "$_executor_get_Count(T[])",
                "$_executor_set_Count(T[],Int32)",
                "$_executor_get_Zero()");
            _weaver.TotalWeavedProperties.Should().Be(4);
            _weaver.TotalWeavedMethods.Should().Be(0);
        }

        [Fact]
        public void IfATypeHasAnExtensionBlock_ThenItsGroupingTypeIsNotWoven()
        {
            _weaver.Execute();

            var groupingTypes = _module.GetType(typeof(ExtensionBlockTargets).FullName).NestedTypes;
            groupingTypes.Should().HaveCount(3);
            groupingTypes.SelectMany(t => t.Methods).Should().NotContain(m => m.Name.StartsWith("$_executor_"));
            // the parameter exists in the method of the class and in its copy
            _warnings.Should().ContainSingle(w => w.Contains("the parameter 'parameter'") && w.Contains(nameof(ExtensionBlockTargets)));
        }
    }
}
