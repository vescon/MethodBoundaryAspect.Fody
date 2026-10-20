using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Swallows the exception of the method and returns the ReturnValue.
    /// </summary>
    public class SwallowExceptionFlowAttribute : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs arg) => arg.FlowBehavior = FlowBehavior.Continue;
    }

    /// <summary>
    /// Skips the method body and returns a task with the result 7.
    /// </summary>
    public class ReturnTaskInEntryAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.ReturnValue = System.Threading.Tasks.Task.FromResult(7);
            arg.FlowBehavior = FlowBehavior.Return;
        }
    }

    /// <summary>
    /// Skips the method body and returns the ReturnValue.
    /// </summary>
    public class ReturnInEntryFlowAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => arg.FlowBehavior = FlowBehavior.Return;
    }
}
