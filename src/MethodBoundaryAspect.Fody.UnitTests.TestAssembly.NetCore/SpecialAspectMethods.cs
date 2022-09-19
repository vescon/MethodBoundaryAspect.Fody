using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    public class SpecialAspectMethods
    {
        [OnlyOnEntryAspect]
        public void MethodWithAspectOnEntryOnly()
        {
        }

        [OnlyOnExitAspect]
        public void MethodWithAspectOnExitOnly()
        {
        }

        [OnlyOnExceptionAspect]
        public void MethodWithAspectOnExceptionOnly()
        {
        }
    }
}