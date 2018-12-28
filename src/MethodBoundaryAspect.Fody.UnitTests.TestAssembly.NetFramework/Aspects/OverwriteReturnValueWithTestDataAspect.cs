using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Shared.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class OverwriteReturnValueWithTestDataAspect : OnMethodBoundaryAspect
    {
        int _key;

        public OverwriteReturnValueWithTestDataAspect(int key)
        {
            _key = key;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            arg.ReturnValue = new TestData(_key);
        }
    }
}