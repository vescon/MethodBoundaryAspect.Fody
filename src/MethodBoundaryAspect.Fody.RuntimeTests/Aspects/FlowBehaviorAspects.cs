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

    /// <summary>
    /// Records its calls, e.g. to see if the OnExit of an aspect runs when a later aspect skips the method body.
    /// </summary>
    public class RecordFlowAttribute : OnMethodBoundaryAspect
    {
        public static readonly System.Collections.Generic.List<string> Calls = new System.Collections.Generic.List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            lock (Calls)
                Calls.Add("OnEntry");
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            lock (Calls)
                Calls.Add("OnExit");
        }
    }
}
