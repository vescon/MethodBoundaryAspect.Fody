using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class AsyncExceptionOrderMethods
    {
        private readonly List<string> _calls;

        public AsyncExceptionOrderMethods(List<string> calls)
        {
            _calls = calls;
        }

        public List<string> Calls => _calls;

        [RecordOrder]
        public async Task<int> FailBeforeAwait()
        {
            if (_calls != null)
                throw new InvalidOperationException("before the first await");

            await Task.Yield();
            return 1;
        }

        [RecordOrder(Swallow = true)]
        public async Task<int> FailAndSwallow()
        {
            if (_calls != null)
                throw new InvalidOperationException("before the first await");

            await Task.Yield();
            return 1;
        }
    }

    public class RecordOrderAttribute : OnMethodBoundaryAspect
    {
        public bool Swallow { get; set; }

        public override void OnEntry(MethodExecutionArgs arg) => Record(arg, "OnEntry");

        public override void OnException(MethodExecutionArgs arg)
        {
            Record(arg, $"OnException {arg.Exception.GetType().Name} ReturnValue={arg.ReturnValue ?? "null"}");
            if (Swallow)
            {
                arg.ReturnValue = -1;
                arg.FlowBehavior = FlowBehavior.Continue;
            }
        }

        public override void OnExit(MethodExecutionArgs arg) => Record(arg, "OnExit");

        private static void Record(MethodExecutionArgs arg, string call) =>
            ((AsyncExceptionOrderMethods)arg.Instance).Calls.Add(call);
    }
}
