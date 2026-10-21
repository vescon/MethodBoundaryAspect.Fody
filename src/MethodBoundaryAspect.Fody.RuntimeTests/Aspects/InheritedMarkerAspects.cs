using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    [AllowChangingInputArguments]
    [AspectSkipProperties(true)]
    public abstract class MarkedBaseAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            if (arg.Arguments != null && arg.Arguments.Length > 0 && arg.Arguments[0] is int)
                arg.Arguments[0] = 42;
        }
    }

    // the marker attributes of the base aspect apply to it as well
    public class InheritedMarkerAttribute : MarkedBaseAspect
    {
    }
}
