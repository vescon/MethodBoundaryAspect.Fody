using System;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Skips the method body and sets the tag, both in the base class of the aspect which is applied.
    /// </summary>
    public class BaseFlowAspect : OnMethodBoundaryAspect
    {
        public static volatile string TagSeenInDerivedEntry;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = "set in the base class";
            arg.ReturnValue = 42;
            arg.FlowBehavior = FlowBehavior.Return;
        }
    }

    /// <summary>
    /// Calls the OnEntry of the base class and reads the tag it set.
    /// </summary>
    public class DerivedFlowAttribute : BaseFlowAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            base.OnEntry(arg);
            TagSeenInDerivedEntry = (string)arg.MethodExecutionTag;
        }
    }

    /// <summary>
    /// Keeps the Arguments array of the last call.
    /// </summary>
    public class KeepArgumentsAttribute : OnMethodBoundaryAspect
    {
        public static volatile object[] LastArguments;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            LastArguments = arg.Arguments;
        }
    }
}
