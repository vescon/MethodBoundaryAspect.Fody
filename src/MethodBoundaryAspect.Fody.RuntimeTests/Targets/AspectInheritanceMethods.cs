using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    [NameRecordingAspect]
    public class AspectInheritanceBase
    {
        public void OwnMethodOfBase()
        {
        }

        public virtual void ClassAspectOverridden()
        {
        }

        [NameRecordingAspect]
        public virtual void MethodAspectOverridden()
        {
        }
    }

    public class AspectInheritanceDerived : AspectInheritanceBase
    {
        public void OwnMethodOfDerived()
        {
        }

        public override void ClassAspectOverridden()
        {
        }

        public override void MethodAspectOverridden()
        {
        }
    }

    [NameRecordingAspect]
    public interface IAspectInheritance
    {
        void Run();
    }

    public class AspectInheritanceImplementation : IAspectInheritance
    {
        public void Run()
        {
        }
    }
}
