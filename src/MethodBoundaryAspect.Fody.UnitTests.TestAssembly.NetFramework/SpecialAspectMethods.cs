using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
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