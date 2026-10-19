using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Writes the ReturnValue only for some methods (like a cache), so the woven code reads it back for all methods.
    /// </summary>
    public class SometimesWritingReturnValueAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            if (arg.Method.Name.StartsWith("Cached"))
            {
                arg.ReturnValue = 99;
                arg.FlowBehavior = FlowBehavior.Return;
            }
        }
    }

    /// <summary>
    /// Reads the ReturnValue in OnExit and swallows exceptions without setting it.
    /// </summary>
    public class SwallowAndReadReturnValueAspect : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            _ = arg.ReturnValue;
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            arg.FlowBehavior = FlowBehavior.Continue;
        }
    }

    /// <summary>
    /// Swallows exceptions and returns 42.
    /// </summary>
    public class SwallowWithReturnValueAspect : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs arg)
        {
            arg.ReturnValue = 42;
            arg.FlowBehavior = FlowBehavior.Continue;
        }
    }
}
