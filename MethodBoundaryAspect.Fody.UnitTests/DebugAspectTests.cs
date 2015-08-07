using System.Diagnostics;
using System.IO;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestProgram;
using NUnit.Framework;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class DebugAspectTests : MethodBoundaryAspectTestBase
    {
        [Ignore]
        [Test]
        public void IfWeavedTestProgramIsExecuted_ThenTheDebugSymbolsShouldWorkAndTheDebuggerShouldBeAttachable()
        {
            // 1) Run unittest without debugger, then in opened dialog attach with current visual studio instance.
            // 2) Put breakpoint in LogAttribute
            
            // Arrange
            var weavedProgramPath = WeaveAssembly(typeof (TestClass));

            // Act
            var process = Process.Start(weavedProgramPath);
            process.WaitForExit();

            // Arrange
            process.ExitCode.Should().Be(0);
        }

        [Ignore]
        [Test]
        public void IfAssemblyIsWeaved_ThenWeaverDebuggerShouldBePossible()
        {
            const string assemblyPath = @"C:\Users\r.kornelius\Desktop\Vescon.So.Server.Jobs.General.dll";

            // Arrange
            Weaver = new ModuleWeaver();
            Weaver.AddClassFilter("Vescon.So.Server.Jobs.General.Translations.GeneralTranslations");
            //Weaver.AddMethodFilter("Vescon.So.Server.Services.Layout.PageIncrementalService.HandleSaveAttributeValuesCommand");
            Weaver.AddAdditionalAssemblyResolveFolder(Path.GetDirectoryName(assemblyPath));
            Weaver.WeaveToShadowFile(assemblyPath);
        }
    }
}