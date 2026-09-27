using System;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// The MethodExecutionArgs properties read or written by the aspects of a method.
    /// Values which are not used are not provided at runtime (no allocation, boxing or reflection).
    /// </summary>
    [Flags]
    public enum ExecutionArgsUsage
    {
        None = 0,

        /// <summary>get_Arguments: the arguments array has to be created</summary>
        Arguments = 1,

        /// <summary>get_ReturnValue: the return value has to be stored before OnExit</summary>
        ReadReturnValue = 2,

        /// <summary>set_ReturnValue: the return value has to be read back after the aspect calls</summary>
        WriteReturnValue = 4,

        /// <summary>get_Method: the MethodBase has to be provided</summary>
        Method = 8,

        /// <summary>get_Instance: the instance has to be provided</summary>
        Instance = 16,

        All = Arguments | ReadReturnValue | WriteReturnValue | Method | Instance
    }
}
