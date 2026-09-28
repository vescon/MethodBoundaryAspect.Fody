using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    // https://github.com/vescon/MethodBoundaryAspect.Fody/issues/19
    // an aspect restricted to public methods, the aspect records which methods are weaved

    [VisibilityRecordingAspect(AttributeTargetMemberAttributes = MulticastAttributes.Public)]
    public class PublicOnlyMethods
    {
        public string PublicProperty { get; set; }

        private string PrivateProperty { get; set; }

        public void CallAll()
        {
            PrivateMethod();
            ProtectedMethod();
            InternalMethod();
            PrivateProtectedMethod();
            ProtectedInternalMethod();
            PrivateStaticMethod();
            PrivateProperty = PrivateProperty;
        }

        public static void PublicStaticMethod() { }

        public async Task PublicAsyncMethod() => await Task.Yield();

        private async Task PrivateAsyncMethod() => await Task.Yield();

        public Task CallPrivateAsyncMethod() => PrivateAsyncMethod();

        private void PrivateMethod() { }
        protected void ProtectedMethod() { }
        internal void InternalMethod() { }
        private protected void PrivateProtectedMethod() { }
        protected internal void ProtectedInternalMethod() { }
        private static void PrivateStaticMethod() { }
    }

    public class PublicOnlyMethodAspects
    {
        [VisibilityRecordingAspect(AttributeTargetMemberAttributes = MulticastAttributes.Public)]
        public void PublicMethod() => PrivateMethod();

        [VisibilityRecordingAspect(AttributeTargetMemberAttributes = MulticastAttributes.Public)]
        private void PrivateMethod() { }
    }
}
