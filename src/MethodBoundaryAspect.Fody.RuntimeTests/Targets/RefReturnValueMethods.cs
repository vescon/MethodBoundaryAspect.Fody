using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class RefReturnValueMethods
    {
        public readonly int ReadOnlyField = 7;
        public int Field = 7;
        public string Text = "text";

        [SetIntReturnValue]
        public ref int Writable() => ref Field;

        [SetIntReturnValue]
        public ref readonly int ReadOnly() => ref ReadOnlyField;

        [SetNullReturnValue]
        public ref int NullForValueType() => ref Field;

        [SetNullReturnValue]
        public ref string NullForReferenceType() => ref Text;
    }

    public class RefReturnValueVirtualBase
    {
        public readonly int ReadOnlyField = 7;

        public virtual ref readonly int ReadOnly() => ref ReadOnlyField;
    }

    public class RefReturnValueVirtualMethods : RefReturnValueVirtualBase
    {
        [SetIntReturnValue]
        public override ref readonly int ReadOnly() => ref ReadOnlyField;
    }
}
