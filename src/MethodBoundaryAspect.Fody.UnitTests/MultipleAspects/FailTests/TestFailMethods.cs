namespace MethodBoundaryAspect.Fody.UnitTests.MultipleAspects.FailTests
{
    public class TestFailMethods
    {
        [RoleAndSameOrderingAspect]
        public void VoidEmptyMethod()
        {
        }

        [FirstAspect]
        [SecondAspect]
        [UnorderedAspect]
        public void VoidEmptyMethodUnorderedAspectMixedWithOrderedAspect()
        {
        }
    }
}