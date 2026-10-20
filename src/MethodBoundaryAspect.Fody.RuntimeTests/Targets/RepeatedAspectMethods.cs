using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    [NameRecordingAspect]
    public class AspectOnClassAndMethod
    {
        [NameRecordingAspect]
        public void Both()
        {
        }

        public void ClassOnly()
        {
        }
    }

    public class ExceptionInEntryAndExit
    {
        public static readonly System.Collections.Generic.List<string> Calls = new System.Collections.Generic.List<string>();

        [ThrowingBoundaryAspect]
        public void Method() => Calls.Add("body");
    }

    public class ThrowingBoundaryAspectAttribute : Attributes.OnMethodBoundaryAspect
    {
        public override void OnEntry(Attributes.MethodExecutionArgs arg) => ExceptionInEntryAndExit.Calls.Add("OnEntry");

        public override void OnExit(Attributes.MethodExecutionArgs arg)
        {
            ExceptionInEntryAndExit.Calls.Add("OnExit");
            throw new InvalidOperationException("from OnExit");
        }

        public override void OnException(Attributes.MethodExecutionArgs arg) => ExceptionInEntryAndExit.Calls.Add("OnException");
    }
}
