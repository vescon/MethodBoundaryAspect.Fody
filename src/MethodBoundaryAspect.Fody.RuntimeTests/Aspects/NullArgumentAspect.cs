using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Sets an argument to null, which is the default value for a value type.
    /// </summary>
    [AllowChangingInputArguments]
    public class NullArgumentAttribute : OnMethodBoundaryAspect
    {
        public int Index { get; set; }

        public override void OnEntry(MethodExecutionArgs arg) => arg.Arguments[Index] = null;
    }
}
