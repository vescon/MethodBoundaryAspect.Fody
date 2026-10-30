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
    /// .NET 11 can compile async methods with runtime-async: there is no state machine, the method has the flag
    /// MethodImplAttributes.Async (0x2000). The sync weaver would move the body into the executor method and leave
    /// the flag at the original method, so these methods are not touched and a warning is written. The flag is set
    /// on a method of an existing test assembly, the .NET 10 compiler can't produce it.
    /// </summary>
    public class RuntimeAsyncMethodTests
    {
        private const MethodImplAttributes RuntimeAsync = (MethodImplAttributes)0x2000;

        private readonly List<string> _warnings = new List<string>();
        private readonly ModuleWeaver _weaver;

        public RuntimeAsyncMethodTests()
        {
            var module = WeaverTestHelper.ReadModule(typeof(VarArgMethods));
            module.GetType(typeof(VarArgMethods).FullName).Methods
                .Single(m => m.Name == nameof(VarArgMethods.Normal)).ImplAttributes |= RuntimeAsync;

            _weaver = new ModuleWeaver { ModuleDefinition = module }.CaptureWarnings(_warnings);
            _weaver.AddClassFilter(typeof(VarArgMethods).FullName);
            _weaver.Execute();
        }

        [Fact]
        public void IfAMethodIsCompiledWithRuntimeAsync_ThenAWarningIsWritten()
        {
            _warnings.Should().ContainSingle(w => w.Contains("runtime-async"))
                .Which.Should().Contain(nameof(VarArgMethods.Normal))
                .And.Contain("is not weaved")
                .And.Contain("runtime-async")
                .And.Contain("[DisableWeaving]");
        }

        [Fact]
        public void IfAMethodIsCompiledWithRuntimeAsync_ThenItIsNotChanged()
        {
            var methods = _weaver.ModuleDefinition.GetType(typeof(VarArgMethods).FullName).Methods;

            methods.Select(m => m.Name).Should().NotContain("$_executor_" + nameof(VarArgMethods.Normal));
            _weaver.TotalWeavedMethods.Should().Be(0);
        }
    }
}
