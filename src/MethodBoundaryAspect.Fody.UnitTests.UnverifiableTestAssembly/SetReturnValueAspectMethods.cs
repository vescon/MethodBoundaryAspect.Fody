using MethodBoundaryAspect.Fody.UnitTests.UnverifiableTestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.UnverifiableTestAssembly
{
    public class SetReturnValueAspectMethods
    {
        public static object Result { get; set; }

        public static int Field = 10;

        public void InstanceMethodCall_RefReturnValueType()
        {
            RefReturnMethodCall();
        }

        [SetReturnValueAspect]
        ref int RefReturnMethodCall()
        {
            return ref Field;
        }

        public static string StrField = "overwritten";

        public void InstanceMethodCall_RefReturnReferenceType()
        {
            RefReturnStringCall();
        }

        [SetReturnValueAspect]
        ref string RefReturnStringCall()
        {
            return ref StrField;
        }

        static int m_field;

        public void TestRefReturnValueOverwritten()
        {
            m_field = 10;
            ref int i = ref GetRef();
            Result = i.ToString();
        }

        [OverwriteReturnValueWithRefIntAspect(42)]
        ref int GetRef()
        {
            return ref m_field;
        }
    }
}
