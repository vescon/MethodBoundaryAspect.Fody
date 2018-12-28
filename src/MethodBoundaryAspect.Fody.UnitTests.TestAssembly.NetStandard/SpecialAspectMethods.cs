using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetStandard.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetStandard
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