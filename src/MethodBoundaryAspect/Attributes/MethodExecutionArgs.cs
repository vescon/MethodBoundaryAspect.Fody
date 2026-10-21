using System;
using System.Reflection;

namespace MethodBoundaryAspect.Fody.Attributes
{
    /// <summary>
    /// This class contains various information about method boundary context.
    /// All aspects of a method get the same instance for one call. The weaver only provides the properties the
    /// aspects of the method use (<see cref="Arguments"/>, <see cref="ReturnValue"/>, <see cref="Method"/>,
    /// <see cref="Instance"/>), the others are <c>null</c>, see the section Performance of the README.
    /// </summary>
    public class MethodExecutionArgs
    {
        /// <summary>
        /// This property defines the behavior after the aspect method calls.
        /// </summary>
        public FlowBehavior FlowBehavior { get; set; }

        /// <summary>
        /// This property contains the instance of the called method.
        /// If a static method is called this property is NULL, and for a method of a ref struct.
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
        /// The elements for ref structs and pointers are NULL, they cannot be boxed.
        /// </summary>
        public object[] Arguments { get; set; }

        /// <summary>
        /// This property will be used as the return value for the current method call.
        /// This property can be modified in the aspect. For an async method it is the returned Task in OnExit,
        /// and the result of the Task if OnException swallows the exception. NULL for a ref struct or pointer.
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