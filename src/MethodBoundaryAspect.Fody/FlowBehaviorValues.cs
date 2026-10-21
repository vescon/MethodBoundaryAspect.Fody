namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// The values of <c>MethodBoundaryAspect.Fody.Attributes.FlowBehavior</c>. The weaver does not reference the runtime
    /// library, it compares the value of <c>MethodExecutionArgs.FlowBehavior</c> with these numbers in the woven code.
    /// </summary>
    public static class FlowBehaviorValues
    {
        public const int Default = 0;
        public const int Continue = 1;
        public const int RethrowException = 2;
        public const int Return = 3;
    }
}
