using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    [MethodAttributeAspect(AttributeTargetMemberAttributes = MulticastAttributes.Internal)]
    public class InternalMethodAttributeAspectMethods
    {
        public static string PublicMethod() => ProtectedInternalMethod();

        protected internal static string ProtectedInternalMethod() => PrivateProtectedMethod();

        private protected static string PrivateProtectedMethod() => InternalMethod();

        internal static string InternalMethod() => ProtectedMethod();

        protected static string ProtectedMethod() => PrivateMethod();

        private static string PrivateMethod() => string.Empty;
    }
}