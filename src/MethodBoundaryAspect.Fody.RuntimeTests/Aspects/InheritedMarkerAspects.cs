using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// The marker attributes of an aspect are inherited by the aspects derived from it, like attributes
    /// are for reflection.
    /// </summary>
    [AllowChangingInputArguments]
    public abstract class ChangingInputBaseAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => arg.Arguments[0] = 42;
    }

    public class ChangingInputDerivedAspect : ChangingInputBaseAspect
    {
    }

    public class ChangingInputDerivedTwiceAspect : ChangingInputDerivedAspect
    {
    }

    [AspectSkipProperties(true)]
    public abstract class SkipPropertiesBaseAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            lock (Calls)
                Calls.Add(arg.Method.Name);
        }
    }

    public class SkipPropertiesDerivedAspect : SkipPropertiesBaseAspect
    {
    }

    [AspectSkipProperties(false)]
    public class SkipPropertiesOverriddenAspect : SkipPropertiesBaseAspect
    {
    }
}
