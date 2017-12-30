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
            const string assemblyPath = @"C:\Dev\So\Main\Source\Vescon.So\Vescon.So.Server.Business\bin\Debug\Vescon.So.Server.Business.dll";

            // Arrange
            Weaver = new ModuleWeaver();
            Weaver.AddClassFilter("Vescon.So.Server.Business.Services.Layout.CommandHandlerBase");
            //Weaver.AddMethodFilter("Vescon.So.Server.Services.Layout.PageIncrementalService.HandleSaveAttributeValuesCommand");
            Weaver.AddAdditionalAssemblyResolveFolder(Path.GetDirectoryName(assemblyPath));
            Weaver.WeaveToShadowFile(assemblyPath);
        }
    }
}