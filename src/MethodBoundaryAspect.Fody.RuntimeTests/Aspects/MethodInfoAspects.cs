using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;
using System.Reflection;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public class MethodInfoRecordingAspect : OnMethodBoundaryAspect
    {
        public static readonly List<MethodBase> Methods = new List<MethodBase>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            lock (Methods)
                Methods.Add(arg.Method);
        }
    }
}
