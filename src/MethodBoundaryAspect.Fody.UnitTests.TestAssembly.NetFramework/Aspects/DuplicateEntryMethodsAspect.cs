using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    /// <summary>
    /// remark: weaved methods with parameter MethodExecutionArgs should be ordered last
    /// </summary>
    public class DuplicateEntryMethodsAspect : OnMethodBoundaryAspect
    {
        public virtual void OnEntry(MethodExecutionArgs arg, bool b)
        {
            DuplicateEntryMethodsAspectMethods.Result += "-OnEntry(bool)";
        }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            DuplicateEntryMethodsAspectMethods.Result += "-OnEntry()";
        }

        public virtual void OnEntry<T>(MethodExecutionArgs arg)
        {
            DuplicateEntryMethodsAspectMethods.Result += "-OnEntry<T>";
        }

        public virtual void OnExit(MethodExecutionArgs arg, bool b)
        {
            DuplicateEntryMethodsAspectMethods.Result += "-OnExit(bool)";
        }

        public virtual void OnExit<T>(MethodExecutionArgs arg)
        {
            DuplicateEntryMethodsAspectMethods.Result += "-OnExit<T>";
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            DuplicateEntryMethodsAspectMethods.Result += "-OnExit()";
        }

        public virtual void OnException(MethodExecutionArgs arg, bool b)
        {
            DuplicateEntryMethodsAspectMethods.Result += "-OnException(bool)";
        }

        public virtual void OnException<T>(MethodExecutionArgs arg)
        {
            DuplicateEntryMethodsAspectMethods.Result += "-OnException<T>";
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            DuplicateEntryMethodsAspectMethods.Result += "-OnException()";
        }
    }
}