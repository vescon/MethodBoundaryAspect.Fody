using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;
using System.Threading;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Holds no state (values of the attribute only), so the weaver shares one instance between all calls.
    /// Tests using the aspects belong to the collection <c>[Collection(nameof(InstanceReuseAspects))]</c>.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class SharedAspectAttribute : OnMethodBoundaryAspect
    {
        private readonly string _name;

        public SharedAspectAttribute(string name)
        {
            _name = name;
        }

        public int Level { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            InstanceReuseAspects.Record(_name + Level + ":entry:" + arg.Method.Name);
            arg.MethodExecutionTag = arg.Arguments.Length > 0 ? arg.Arguments[0] : null;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            // the tag belongs to the call, not to the shared aspect
            if (arg.Arguments.Length > 0 && !Equals(arg.MethodExecutionTag, arg.Arguments[0]))
                Interlocked.Increment(ref InstanceReuseAspects.MixedUpTags);

            InstanceReuseAspects.Record(_name + Level + ":exit:" + arg.Method.Name);
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            InstanceReuseAspects.Record(_name + Level + ":exception:" + arg.Exception.Message);
        }
    }

    /// <summary>
    /// Only applied to one class, to find the one shared instance of it.
    /// </summary>
    public class SharedClassAspectAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            InstanceReuseAspects.Record("class:entry:" + arg.Method.Name);
        }
    }

    /// <summary>
    /// Counts its calls in an instance field: it has to be created for every call.
    /// </summary>
    public class PerCallStateAttribute : OnMethodBoundaryAspect
    {
        private int _calls;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            _calls++;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            InstanceReuseAspects.Record("calls:" + _calls);
        }
    }

    public static class InstanceReuseAspects
    {
        public static readonly List<string> Calls = new List<string>();

        public static int MixedUpTags;

        public static void Record(string text)
        {
            lock (Calls)
                Calls.Add(text);
        }
    }
}
