using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Mono.Cecil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// The executor method of a woven method does not get the variable arguments (__arglist) of the method, so
    /// they would be lost silently. These methods are not touched, a warning is written.
    /// </summary>
    public class VarArgMethodTests
    {
        private readonly List<string> _warnings = new List<string>();
        private readonly ModuleWeaver _weaver;

        public VarArgMethodTests()
        {

            _weaver = new ModuleWeaver
            {
                ModuleDefinition = WeaverTestHelper.ReadModule(typeof(VarArgMethods))
            }.CaptureWarnings(_warnings);
            _weaver.AddClassFilter(typeof(VarArgMethods).FullName);
            _weaver.Execute();
        }

        [Fact]
        public void IfAMethodHasAVariableArgumentList_ThenAWarningIsWritten()
        {
            _warnings.Should().ContainSingle()
                .Which.Should().Contain(nameof(VarArgMethods.VarArg))
                .And.Contain("is not weaved")
                .And.Contain("__arglist")
                .And.Contain("[DisableWeaving]");
        }

        [Fact]
        public void IfAMethodHasAVariableArgumentList_ThenItIsNotChanged()
        {
            var methods = _weaver.ModuleDefinition.GetType(typeof(VarArgMethods).FullName).Methods;

            methods.Select(m => m.Name).Should().NotContain("$_executor_" + nameof(VarArgMethods.VarArg));
            methods.Single(m => m.Name == nameof(VarArgMethods.VarArg)).Body.Instructions.Count.Should().BeLessThan(5);
        }

        [Fact]
        public void IfOtherMethodsHaveNoVariableArgumentList_ThenTheyAreWeaved()
        {
            var methods = _weaver.ModuleDefinition.GetType(typeof(VarArgMethods).FullName).Methods;

            methods.Select(m => m.Name).Should().Contain("$_executor_" + nameof(VarArgMethods.Normal));
            _weaver.TotalWeavedMethods.Should().Be(1);
        }
    }
}
