using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class InvalidArgumentMethods
    {
        public bool Called;

        [InvalidFirstArgument]
        public int ByValue(int value)
        {
            Called = true;
            return value;
        }

        [InvalidFirstArgument]
        public void ByRef(ref int value)
        {
            Called = true;
        }
    }
}
