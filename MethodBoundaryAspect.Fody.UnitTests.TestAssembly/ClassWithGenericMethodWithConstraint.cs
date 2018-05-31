using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class ClassWithGenericMethodWithConstraint
    {
        [SetInstanceValueAspect]
        public void DoIt<T>(T testclass) where T : class 
        {
        }
    }
}