using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Sets the first argument to a string, which is not valid for an int.
    /// </summary>
    [AllowChangingInputArguments]
    public class InvalidFirstArgumentAttribute : OnMethodBoundaryAspect
    {
        public static readonly List<string> OnExceptionCalls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg) => arg.Arguments[0] = "not a number";

        public override void OnException(MethodExecutionArgs arg)
        {
            lock (OnExceptionCalls)
                OnExceptionCalls.Add(arg.Exception.GetType().Name);
        }
    }
}
