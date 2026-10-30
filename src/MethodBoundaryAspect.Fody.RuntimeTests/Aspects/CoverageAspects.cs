using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public static class CoverageLog
    {
        public static readonly List<string> Calls = new List<string>();

        public static readonly ConcurrentBag<MethodBase> Methods = new ConcurrentBag<MethodBase>();

        public static TaskCompletionSource<string> SwallowedException = new TaskCompletionSource<string>();

        public static void Record(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }

        public static void Reset()
        {
            lock (Calls)
                Calls.Clear();
            SwallowedException = new TaskCompletionSource<string>();
        }
    }

    public sealed class CoverageRecordingAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => CoverageLog.Record("OnEntry " + arg.Method.Name);

        public override void OnExit(MethodExecutionArgs arg) => CoverageLog.Record("OnExit " + arg.Method.Name);
    }

    /// <summary>Swallows the exception (and tells the test), for methods with and without result.</summary>
    public sealed class CoverageSwallowAspect : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs arg)
        {
            arg.FlowBehavior = FlowBehavior.Continue;
            CoverageLog.SwallowedException.TrySetResult(arg.Exception.Message);
        }
    }

    /// <summary>Skips the method body, the return value is the one of the aspect (if the method returns an int-like task).</summary>
    public sealed class CoverageReturnAspect : OnMethodBoundaryAspect
    {
        public int Result { get; set; } = -1;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.FlowBehavior = FlowBehavior.Return;
            if (Result >= 0)
                arg.ReturnValue = new ValueTask<int>(Result);
        }
    }

    /// <summary>Sets a FlowBehavior in OnEntry which has no effect there: the method body runs.</summary>
    public sealed class CoverageNoEffectAspect : OnMethodBoundaryAspect
    {
        public FlowBehavior Behavior { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.FlowBehavior = Behavior;
        }
    }

    /// <summary>Keeps the method info of every call, to compare what concurrent first calls get.</summary>
    public sealed class CoverageMethodAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => CoverageLog.Methods.Add(arg.Method);
    }
}
