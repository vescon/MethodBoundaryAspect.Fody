using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Replaces the first argument (an int) by 99.
    /// </summary>
    [AllowChangingInputArguments]
    public class ChangeFirstArgumentTo99Attribute : OnMethodBoundaryAspect
    {
        public static readonly List<string> ArgumentsInOnExit = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg) => arg.Arguments[0] = 99;

        public override void OnExit(MethodExecutionArgs arg)
        {
            lock (ArgumentsInOnExit)
                ArgumentsInOnExit.Add(arg.Arguments[0]?.ToString());
        }
    }

    /// <summary>
    /// Replaces the first argument (an int) by 99 and swallows the exception of the method.
    /// </summary>
    [AllowChangingInputArguments]
    public class ChangeFirstArgumentAndSwallowAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => arg.Arguments[0] = 99;

        public override void OnException(MethodExecutionArgs arg) => arg.FlowBehavior = FlowBehavior.Continue;
    }
}
