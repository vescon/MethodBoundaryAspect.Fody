using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody.UnitTests.Unified
{
    public class UnifiedWeaverTestBase : IDisposable
    {
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
        
        protected AssemblyPaths Source { get; private set; }
        protected AssemblyPaths Weave { get; private set; }

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

        protected void AssertRunPeVerify()
        {
            Action action = () => PeVerifier.Verify(Weave.DllPath);
            action.ShouldNotThrow();
        }

        public void Dispose()
        {
            TryCleanupWeavedFiles(Weave);
        }

        private void TryCleanupWeavedFiles(AssemblyPaths assemblyPaths)
        {
            if (File.Exists(assemblyPaths.DllPath))
                File.Delete(assemblyPaths.DllPath);

            if (File.Exists(assemblyPaths.PdbPath))
                File.Delete(assemblyPaths.PdbPath);
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
                var dllPath = unweavedAssemblyPaths.DllPath.Replace(".DLL", "._Weaved.dll");
                var pdbPath = unweavedAssemblyPaths.PdbPath.Replace(".Pdb", "._Weaved.Pdb");

                return new AssemblyPaths(dllPath, pdbPath);
            }
        }
    }
}