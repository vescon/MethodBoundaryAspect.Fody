using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    // https://github.com/vescon/MethodBoundaryAspect.Fody/issues/132
    // each class calls a method of every visibility, the aspect records which of them are weaved

    [VisibilityRecordingAspect(AttributeTargetMemberAttributes = MulticastAttributes.AnyVisibility)]
    public class AnyVisibilityMethods
    {
        public void CallAll()
        {
            PrivateMethod();
            ProtectedMethod();
            InternalMethod();
            PrivateProtectedMethod();
            ProtectedInternalMethod();
        }

        private void PrivateMethod() { }
        protected void ProtectedMethod() { }
        internal void InternalMethod() { }
        private protected void PrivateProtectedMethod() { }
        protected internal void ProtectedInternalMethod() { }
    }

    [VisibilityRecordingAspect(AttributeTargetMemberAttributes = MulticastAttributes.Default)]
    public class DefaultVisibilityMethods
    {
        public void CallAll()
        {
            PrivateMethod();
            ProtectedMethod();
            InternalMethod();
            PrivateProtectedMethod();
            ProtectedInternalMethod();
        }

        private void PrivateMethod() { }
        protected void ProtectedMethod() { }
        internal void InternalMethod() { }
        private protected void PrivateProtectedMethod() { }
        protected internal void ProtectedInternalMethod() { }
    }

    [VisibilityRecordingAspect(AttributeTargetMemberAttributes = MulticastAttributes.Public | MulticastAttributes.Internal)]
    public class PublicAndInternalMethods
    {
        public void CallAll()
        {
            PrivateMethod();
            ProtectedMethod();
            InternalMethod();
            PrivateProtectedMethod();
            ProtectedInternalMethod();
        }

        private void PrivateMethod() { }
        protected void ProtectedMethod() { }
        internal void InternalMethod() { }
        private protected void PrivateProtectedMethod() { }
        protected internal void ProtectedInternalMethod() { }
    }

    [VisibilityRecordingAspect]
    public class NotConfiguredVisibilityMethods
    {
        public void CallAll()
        {
            PrivateMethod();
            ProtectedMethod();
            InternalMethod();
            PrivateProtectedMethod();
            ProtectedInternalMethod();
        }

        private void PrivateMethod() { }
        protected void ProtectedMethod() { }
        internal void InternalMethod() { }
        private protected void PrivateProtectedMethod() { }
        protected internal void ProtectedInternalMethod() { }
    }
}
