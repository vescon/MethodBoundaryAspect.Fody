using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    // see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/120
    public abstract class InheritedAspectBase : OnMethodBoundaryAspect
    {
        protected static void Log(string message)
        {
            InheritedAspectMethods.Result += message;
        }
    }

    public class InheritedAspect : InheritedAspectBase
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            Log("[InheritedAspect_OnEntry]");
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            Log($"[InheritedAspect_OnExit:{arg.ReturnValue}]");
        }
    }

    public abstract class InheritedOnEntryAspectBase : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            InheritedAspectMethods.Result += "[InheritedOnEntryAspectBase_OnEntry]";
        }
    }

    public class InheritedOnExitAspect : InheritedOnEntryAspectBase
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            InheritedAspectMethods.Result += "[InheritedOnExitAspect_OnExit]";
        }
    }
}
