using System;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class FlowBehaviorClass
    {
        [FlowBehaviorAspect(Behavior = FlowBehavior.Continue, ReturnValue = "Changed")]
        public string SuppressAndReturnString()
        {
            string s = "Original";
            if (s.Length > 1)
                throw new Exception("An exception");
            return s;
        }

        [FlowBehaviorAspect(Behavior = FlowBehavior.Continue, ReturnValue = 42)]
        public int SuppressAndReturnValueType()
        {
            int i = -1;
            if (i < 0)
                throw new Exception("An exception");
            return i;
        }

        [FlowBehaviorAspect(Behavior = FlowBehavior.Default)]
        public void Default()
        {
            throw new Exception("An exception");
        }

        [FlowBehaviorAspect(Behavior=FlowBehavior.RethrowException)]
        public void Rethrow()
        {
            throw new Exception("An exception");
        }

        [FlowBehaviorAspect(Behavior =FlowBehavior.Continue)]
        public void Suppress()
        {
            throw new Exception("An exception");
        }

        [FlowBehaviorAspect(Behavior = FlowBehavior.Return)]
        public void SuppressReturn()
        {
            throw new Exception("An exception");
        }

        [FlowBehaviorOnEntryAspect(FlowBehavior=FlowBehavior.Return, ReturnValue =3)]
        public int TryReturn()
        {
            return 0;
        }

        [FlowBehaviorOnEntryAspect(FlowBehavior=FlowBehavior.Return)]
        public void TryReturnVoid()
        {
            throw new Exception("Should not be thrown.");
        }

        [FlowBehaviorOnEntryAspect(FlowBehavior = FlowBehavior.Return, ReturnValue = 6)]
        [FlowBehaviorOnEntry2Aspect(ReturnValue = 9)]
        public int AbortAspects() => 0;

        [FlowBehaviorOnEntryAndLogOnExitAspect(ReturnValue = 6)]
        [FlowBehaviorOnEntry2Aspect(FlowBehavior = FlowBehavior.Return, ReturnValue = 9)]
        public int ExitTest() => 0;

        [ThrowIfOnExceptionIsCalledAspect(1, ReturnValue = 6)]
        [ExitAfterExceptionAspect]
        public int ExitAfterException()
        {
            throw new Exception("Testing OnException");
        }

        public int TestExitAfterAsyncException()
        {
            var task = ExitAfterAsyncException();
            return task.Result;
        }

        // allow OnExit for exiting the actual method, but not for exiting the state machine.
        [ThrowIfOnExceptionIsCalledAspect(1)]
        [ExitAfterExceptionAspect(1, ReturnValue=6)]
        public async Task<int> ExitAfterAsyncException()
        {
            await Task.Delay(1);
            throw new Exception("Testing OnException");
        }
    }
}
