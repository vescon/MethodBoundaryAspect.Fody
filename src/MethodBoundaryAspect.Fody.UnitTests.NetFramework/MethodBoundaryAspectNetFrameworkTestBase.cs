using System;
using MethodBoundaryAspect.Fody.UnitTests.Shared;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class MethodBoundaryAspectNetFrameworkTestBase : MethodBoundaryAspectTestBase
    {
        private TestDomain _testDomain;
        
        protected AssemblyLoader AssemblyLoader { get; private set; }
                
        public override void Dispose()
        {
            AssemblyLoader = null;

            if (_testDomain != null)
            {
                _testDomain.Dispose();
                _testDomain = null;
            }

            base.Dispose();
        }
        
        protected void WeaveAssemblyClassWithNestedTypesAndLoad(Type type)
        {
            WeaveAssemblyAndVerify(type, null, null, true);
            LoadWeavedAssembly();
        }

        protected void WeaveAssemblyClassAndLoad(Type type)
        {
            WeaveAssemblyAndVerify(type, null, null, false);
            LoadWeavedAssembly();
        }

        protected void WeaveAssemblyMethodAndLoad(Type type, string methodName)
        {
            WeaveAssemblyAndVerify(type, methodName, null, false);
            LoadWeavedAssembly();
        }

        protected void WeaveAssemblyPropertyAndLoad(Type type, string propertyName)
        {
            WeaveAssemblyAndVerify(type, null, propertyName, false);
            LoadWeavedAssembly();
        }
        
        private void LoadWeavedAssembly()
        {
            _testDomain = new TestDomain();

            AssemblyLoader = _testDomain.CreateAssemblyLoader();
            AssemblyLoader.SetDomain(_testDomain.AppDomain);
            AssemblyLoader.Load(WeavedAssemblyPath);
        }
    }
}