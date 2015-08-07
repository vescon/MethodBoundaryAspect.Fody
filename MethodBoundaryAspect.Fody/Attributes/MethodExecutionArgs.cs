using System;
using System.Reflection;

namespace MethodBoundaryAspect.Fody.Attributes
{
    public class MethodExecutionArgs
    {
        public object Instance { get; set; }
        public MethodBase Method { get; set; }
        public object[] Arguments { get; set; }
        public object ReturnValue { get; set; }
        public Exception Exception { get; set; }
        public object MethodExecutionTag { get; set; }
    }
}