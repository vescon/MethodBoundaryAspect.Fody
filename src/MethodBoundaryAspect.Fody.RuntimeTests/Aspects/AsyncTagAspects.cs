using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// The pattern of the README: OnException only handles exceptions thrown before the method returned its task,
    /// tracked with the MethodExecutionTag. Tests using it belong to the collection
    /// <c>[Collection(nameof(ExitFlagAttribute))]</c>.
    /// </summary>
    public class ExitFlagAttribute : OnMethodBoundaryAspect
    {
        public static readonly List<string> Seen = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = false;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = true;
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            lock (Seen)
                Seen.Add("OnException tag=" + arg.MethodExecutionTag);
        }
    }

    /// <summary>
    /// A second aspect on the method: with several aspects each one gets its own tag.
    /// </summary>
    public class OtherAspectWithTagAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = "other";
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
        }
    }
}
