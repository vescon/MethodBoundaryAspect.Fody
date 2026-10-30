using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class ByRefResultMethods
    {
        [RecordFirstArgument]
        public void Out(out int value) => value = 42;

        [RecordFirstArgument]
        public void Ref(ref int value) => value += 1;
    }
}
