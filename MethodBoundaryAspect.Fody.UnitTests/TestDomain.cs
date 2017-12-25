using System;
using System.Security.Policy;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class TestDomain : IDisposable
    {
        private readonly AppDomain _appDomain;

        public TestDomain()
        {
            var appSetup = new AppDomainSetup {ApplicationBase = Environment.CurrentDirectory};

            // Set up the Evidence
            var baseEvidence = AppDomain.CurrentDomain.Evidence;
            var evidence = new Evidence(baseEvidence);

            _appDomain = AppDomain.CreateDomain("TestDomain", evidence, appSetup);
        }

        public AssemblyLoader CreateAssemblyLoader()
        {
            var assemblyLoaderType = typeof (AssemblyLoader);
            return
                (AssemblyLoader)
                    _appDomain.CreateInstanceAndUnwrap(assemblyLoaderType.Assembly.FullName,
                        assemblyLoaderType.FullName);
        }

        public void Dispose()
        {
            AppDomain.Unload(_appDomain);
        }
    }
}