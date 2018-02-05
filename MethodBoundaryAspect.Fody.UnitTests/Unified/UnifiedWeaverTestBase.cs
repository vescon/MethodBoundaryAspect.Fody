using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody.UnitTests.Unified
{
    public class UnifiedWeaverTestBase : IDisposable
    {
        private const bool ShouldRunIlSpyOnPeVerifyError = false;

        protected static readonly List<OpCode> AllStLocOpCodes = new List<OpCode>
                {
                    OpCodes.Stloc_S,
                    OpCodes.Stloc_0,
                    OpCodes.Stloc_1,
                    OpCodes.Stloc_2,
                    OpCodes.Stloc_3,
                };
        protected static readonly List<OpCode> AllLdLocOpCodes = new List<OpCode>
                {
                    OpCodes.Ldloc_S,
                    OpCodes.Ldloc_0,
                    OpCodes.Ldloc_1,
                    OpCodes.Ldloc_2,
                    OpCodes.Ldloc_3
                };
        
        protected AssemblyPaths Source { get; }
        protected AssemblyPaths Weave { get; }
        
        protected UnifiedWeaverTestBase()
        {
            var source = AssemblyPaths.OfExecutingAssembly();
            var weave = AssemblyPaths.ForWeavedAssembly(source);

            TryCleanupWeavedFiles(weave);

            File.Copy(source.DllPath, weave.DllPath, true);
            File.Copy(source.PdbPath, weave.PdbPath, true);
            
            Source = source;
            Weave = weave;
        }

        protected void AssertRunPeVerify(ModuleWeaver weaver = null)
        {
            Action action = () =>
            {
                try
                {
                    PeVerifier.Verify(Weave.DllPath);
                }
                catch (Exception)
                {
                    var methodName = weaver?.MethodFilters.Single();
                    RunIlSpy(methodName, Weave.DllPath);
                    RunIlSpy(methodName, Source.DllPath);
                    throw;
                }
            };
            action.ShouldNotThrow();
        }

        public void Dispose()
        {
            TryCleanupWeavedFiles(Weave);
        }

        private static void TryCleanupWeavedFiles(AssemblyPaths assemblyPaths)
        {
            if (File.Exists(assemblyPaths.DllPath))
                File.Delete(assemblyPaths.DllPath);

            if (File.Exists(assemblyPaths.PdbPath))
                File.Delete(assemblyPaths.PdbPath);
        }

        private void RunIlSpy(string methodName, string assemblyPath)
        {
            if (!ShouldRunIlSpyOnPeVerifyError)
                return;

            if (methodName == null)
                return;
            
            var args = new[] {assemblyPath, "/separate", "/clearList", "/language:IL", "/navigateTo:M:" + methodName};
            var arg = string.Join(" ", args);

            var currentDirectory = Environment.CurrentDirectory+ @"\..\..\..\..\Tools\ILSpy";
            var psi = new ProcessStartInfo
            {
                WorkingDirectory = currentDirectory ,
                FileName = currentDirectory + @"\ILSpy.exe",
                Arguments = arg
            };

            Process.Start(psi);
        }

        protected class AssemblyPaths
        {
            public AssemblyPaths()
            {
            }

            private AssemblyPaths(string dllPath, string pdbPath)
            {
                DllPath = dllPath;
                PdbPath = pdbPath;
            }

            public string DllPath { get; }
            public string PdbPath { get; }

            public static AssemblyPaths OfExecutingAssembly()
            {
                var dllPath = Assembly.GetExecutingAssembly().CodeBase.Replace(@"file:///", string.Empty);
                var pdbPath = dllPath.Replace(@".DLL", ".Pdb");

                return new AssemblyPaths(dllPath, pdbPath);
            }

            public static AssemblyPaths ForWeavedAssembly(AssemblyPaths unweavedAssemblyPaths)
            {
                var dllPath = unweavedAssemblyPaths.DllPath.Replace(".DLL", ".Weaved.dll");
                var pdbPath = unweavedAssemblyPaths.PdbPath.Replace(".Pdb", ".Weaved.Pdb");

                return new AssemblyPaths(dllPath, pdbPath);
            }
        }
    }
}