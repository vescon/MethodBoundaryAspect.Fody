using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
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