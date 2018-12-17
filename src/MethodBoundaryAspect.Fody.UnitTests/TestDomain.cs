using System;
using System.Security.Policy;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class TestDomain : IDisposable
    {
        public TestDomain()
        {
            var appSetup = new AppDomainSetup {ApplicationBase = Environment.CurrentDirectory};

            // Set up the Evidence
            var baseEvidence = AppDomain.CurrentDomain.Evidence;
            var evidence = new Evidence(baseEvidence);

            AppDomain = AppDomain.CreateDomain("TestDomain", evidence, appSetup);
        }

        public AppDomain AppDomain { get; }

        public AssemblyLoader CreateAssemblyLoader()
        {
            var assemblyLoaderType = typeof (AssemblyLoader);
            return (AssemblyLoader) AppDomain.CreateInstanceAndUnwrap(
                assemblyLoaderType.Assembly.FullName,
                assemblyLoaderType.FullName);
        }

        public void Dispose()
        {
            AppDomain.Unload(AppDomain);
        }
    }
}