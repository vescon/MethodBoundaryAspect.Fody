using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.External.Aspects
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