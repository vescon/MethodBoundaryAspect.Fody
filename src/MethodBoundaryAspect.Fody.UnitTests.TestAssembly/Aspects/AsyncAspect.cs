using System;
using System.Threading;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class AsyncAspect : OnMethodBoundaryAspect
    {
        string _message;

        void CheckArgs(MethodExecutionArgs args)
        {
            var info = args.Method;
            if (info == null)
                throw new ArgumentNullException("Expected non-null method.");
            if (info.Name == "MoveNext")
                throw new Exception("Expected async method info, not state machine.");

            if (!info.IsStatic && !IsRightClass(args.Instance))
                throw new Exception("Expected instance to be set on instance method.");
        }

        public AsyncAspect(string message)
        {
            _message = message;
        }

        public virtual bool IsRightClass(object o) => o is AsyncClass;

        public virtual void Set(string value)
        {
            AsyncClass.Result += value;
        }

        public virtual void Set(int value)
        {
            AsyncClass.Result = value;
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            CheckArgs(arg);
            if (arg.Exception.Message == "An exception")
                Set(" OnException " + _message);
            ((ManualResetEventSlim)arg.MethodExecutionTag).Set();
        }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = new ManualResetEventSlim();
            CheckArgs(arg);
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            CheckArgs(arg);
            if (arg.ReturnValue is Task<int> t)
                Set(t.Result);
            else if (arg.ReturnValue is Task<Placeholder> tp)
            {
                while (!tp.IsCompleted) ;
                if (!tp.IsFaulted)
                    Set(tp.Result.Value);
            }
            ((ManualResetEventSlim)arg.MethodExecutionTag).Wait(100);
        }
    }
}
