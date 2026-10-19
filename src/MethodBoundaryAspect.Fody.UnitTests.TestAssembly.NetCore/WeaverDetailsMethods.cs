using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    public class LastMethodNotWovenMethods
    {
        [OnlyOnEntryAspect]
        public void Woven()
        {
        }

        [NoAspectMethodsAspect]
        public void NotWoven()
        {
        }
    }

    public class PropertyFilterMethods
    {
        [OnlyOnEntryAspect]
        public int Value { get; set; }
    }
}
