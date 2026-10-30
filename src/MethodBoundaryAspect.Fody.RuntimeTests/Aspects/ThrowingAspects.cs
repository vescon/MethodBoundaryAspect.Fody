using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Records its calls and throws in OnEntry or OnExit, to see that OnException only guards the method body.
    /// </summary>
    public class ThrowingBoundaryAttribute : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public bool ThrowInEntry { get; set; }
        public bool ThrowInExit { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Record("OnEntry");
            if (ThrowInEntry)
                throw new InvalidOperationException("entry");
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            Record("OnExit");
            if (ThrowInExit)
                throw new InvalidOperationException("exit");
        }

        public override void OnException(MethodExecutionArgs arg) => Record("OnException");

        private static void Record(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }
}
