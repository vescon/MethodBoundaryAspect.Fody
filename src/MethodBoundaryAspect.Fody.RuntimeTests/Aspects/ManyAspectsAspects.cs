using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Uses all aspect methods and the return value, so the woven exception handler of a method
    /// with several of these aspects becomes large.
    /// </summary>
    public abstract class ManyAspectsRecordingAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = Name;
            Record($"OnEntry {Name}");
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            Record($"OnExit {Name} {arg.MethodExecutionTag} {arg.ReturnValue}");
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            Record($"OnException {Name} {arg.MethodExecutionTag}");
            HandleException(arg);
        }

        protected virtual void HandleException(MethodExecutionArgs arg)
        {
        }

        private string Name => GetType().Name;

        private static void Record(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }

    public class ManyAspectsA : ManyAspectsRecordingAspect { }

    public class ManyAspectsB : ManyAspectsRecordingAspect { }

    public class ManyAspectsC : ManyAspectsRecordingAspect { }

    /// <summary>
    /// Handles the exception by returning 42. Applied last, so its OnException is called first and
    /// jumps over the exception handling code of all other aspects.
    /// </summary>
    public class ManyAspectsReturningD : ManyAspectsRecordingAspect
    {
        protected override void HandleException(MethodExecutionArgs arg)
        {
            arg.ReturnValue = 42;
            arg.FlowBehavior = FlowBehavior.Return;
        }
    }
}
