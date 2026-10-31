using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Assigns a new array to Arguments, which is ignored: only the elements of the original array count.
    /// </summary>
    [AllowChangingInputArguments]
    public class ReplaceArgumentsArrayAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => arg.Arguments = new object[] { 99 };
    }
}
