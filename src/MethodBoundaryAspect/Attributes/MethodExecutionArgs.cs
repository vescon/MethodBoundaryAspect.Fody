using System;
using System.Reflection;

namespace MethodBoundaryAspect.Fody.Attributes
{
    /// <summary>
    /// This class contains various information about method boundary context 
    /// </summary>
    public class MethodExecutionArgs
    {
        /// <summary>
        /// This property defines the behavior after the aspect method calls.
        /// </summary>
        public FlowBehavior FlowBehavior { get; set; }

        /// <summary>
        /// This property contains the instance of the called method.
        /// If a static method is called this property is NULL
        /// </summary>
        public object Instance { get; set; }

        /// <summary>
        /// This property contains the method information of the called method.
        /// </summary>
        public MethodBase Method { get; set; }

        /// <summary>
        /// This property contains the input arguments of the current method call.
        /// If the aspect has the "AllowChangingInputArgumentsAttribute" attribute the aspect can modify
        /// these values. The modified values will then be forwarded to the real method call.
        /// </summary>
        public object[] Arguments { get; set; }

        /// <summary>
        /// This property will be used as the return value for the current method call.
        /// This property can be modified in the aspect.
        /// </summary>
        public object ReturnValue { get; set; }

        /// <summary>
        /// This property contains the exception object if an exception was thrown in the real method call.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// This property can be used to store any data over the lifetime of the method call.
        /// E.g. store an id in "OnEntry()" and use it in the "OnExit()" method call
        /// </summary>
        public object MethodExecutionTag { get; set; }
    }
}