using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class GenericClass<T>
    {
        [SetInstanceValueAspect]
        public void DoIt()
        {
        }

        public void DoItNotWeaved()
        {
        }

        public void CallOther()
        {
            var thisRef = this;
            DoIt();
        }
    }
}