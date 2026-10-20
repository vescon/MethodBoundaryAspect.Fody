namespace MethodBoundaryAspect.Fody.Attributes
{
    /// <summary>
    /// What happens after an aspect method, set in <see cref="MethodExecutionArgs.FlowBehavior"/>.
    /// </summary>
    public enum FlowBehavior
    {
        /// <summary>
        /// The method body runs, an exception is rethrown.
        /// </summary>
        Default,

        /// <summary>
        /// In OnException: the exception is swallowed, the method returns <see cref="MethodExecutionArgs.ReturnValue"/>
        /// (the default value of the return type if no aspect sets it).
        /// </summary>
        Continue,

        /// <summary>
        /// In OnException: the exception is rethrown, like <see cref="Default"/>.
        /// </summary>
        RethrowException,

        /// <summary>
        /// In OnEntry: the method body is skipped and the method returns <see cref="MethodExecutionArgs.ReturnValue"/>.
        /// In OnException: the exception is swallowed, like <see cref="Continue"/>.
        /// </summary>
        Return
    }
}
