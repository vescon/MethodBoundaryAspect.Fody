using System.Diagnostics;
using System.IO;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestProgram;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class DebugAspectTests : MethodBoundaryAspectTestBase
    {
        [Fact(Skip = "Needs to be clarified")]
        public void IfWeavedTestProgramIsExecuted_ThenTheDebugSymbolsShouldWorkAndTheDebuggerShouldBeAttachable()
        {
            // 1) Run unittest without debugger, then in opened dialog attach with current visual studio instance.
            // 2) Put breakpoint in LogAttribute

            // Arrange
            var weavedProgramPath = WeaveAssembly(typeof(TestClass));

            // Act
            var process = Process.Start(weavedProgramPath);
            process.WaitForExit();

            // Arrange
            process.ExitCode.Should().Be(0);
        }

        [Fact(Skip = "Needs to be clarified")]
        public void IfAssemblyIsWeaved_ThenWeaverDebuggerShouldBePossible()
        {
            // Arrange
            var assemblyPath = @"c:\???";

            Weaver = new ModuleWeaver();
            Weaver.AddClassFilter("???");
            Weaver.AddAdditionalAssemblyResolveFolder(Path.GetDirectoryName(assemblyPath));
            Weaver.WeaveToShadowFile(assemblyPath);
        }
    }
}