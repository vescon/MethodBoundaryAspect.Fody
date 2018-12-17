using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;

namespace MethodBoundaryAspect.Fody.UnitTests.Unified
{
    public class UnifiedWeaverTestBase : IDisposable
    {
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

        protected void AssertRunPeVerify(ModuleWeaver weaver)
        {
            Action action = () =>
            {
                var runIlSpy = false;
                try
                {
                    PeVerifier.Verify(Weave.DllPath);
                }
                catch (Exception)
                {
                    runIlSpy = true;
                    throw;
                }
                finally
                {
                    if (runIlSpy || IlSpy.AlwaysRunIlSpy)
                    {
                        var methodName = weaver?.MethodFilters.Single();
                        IlSpy.ShowMethod(methodName, Weave.DllPath);
                        IlSpy.ShowMethod(methodName, Source.DllPath);
                        Thread.Sleep(TimeSpan.FromSeconds(2)); // wait until ILSpy is started because weaved dll will be deleted when unittests exits -> TryCleanupWeavedFiles()
                    }
                }
            };
            action.Should().NotThrow();
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

        protected class AssemblyPaths
        {
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