using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class GenericClassWithConstraintsAndBase<T> : BaseClass
        where T : Entity 
    {
        public void DoIt()
        {
            SomePrivateMethod();
        }
        
        [OnlyOnEntrAndExityAspect]
        private void SomePrivateMethod()
        {
            var thisRef = this;
        }
    }

    public class BaseClass
    {
    }

    public class Entity
    {
    }
}