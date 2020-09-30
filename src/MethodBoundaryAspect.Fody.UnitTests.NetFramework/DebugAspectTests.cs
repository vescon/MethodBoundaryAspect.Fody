using System.Diagnostics;
using System.IO;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestProgram.NetFramework;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class DebugAspectTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        [Fact(Skip = "Should be used only for manual testing")]
        ////[Fact]
        public void IfWeavedTestProgramIsExecuted_ThenTheDebugSymbolsShouldWorkAndTheDebuggerShouldBeAttachable()
        {
            // 1) Run unit test without debugger, then in opened dialog attach with current visual studio instance.
            // 2) Put breakpoint in LogAttribute

            // Arrange
            var weavedProgramPath = WeaveAssembly(typeof(LogTest));

            // Act
            var process = Process.Start(weavedProgramPath);
            process.WaitForExit();

            // Arrange
            process.ExitCode.Should().Be(0);
        }

        [Fact(Skip = "Should be used only for manual testing")]
        public void IfAssemblyIsWeaved_ThenWeaverDebuggerShouldBePossible()
        {
            // Arrange
            const string assemblyPath = @"c:\???";

            Weaver = new ModuleWeaver();
            Weaver.AddClassFilter("???");
            Weaver.WeaveToShadowFile(assemblyPath, new FolderAssemblyResolver(ModuleHelper.AssemblyResolver, Path.GetDirectoryName(assemblyPath)));
        }
    }
}