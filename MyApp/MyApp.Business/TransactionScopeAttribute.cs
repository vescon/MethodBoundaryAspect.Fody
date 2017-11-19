using MethodBoundaryAspect.Fody.Attributes;
using System.Diagnostics;

namespace MyApp.Business
{
    public sealed class TransactionScopeAttribute : OnMethodBoundaryAspect
    {
        public int TimeoutInSeconds { get; set; }

        public override void OnEntry(MethodExecutionArgs args)
        {
            Debug.WriteLine("OnEntry");
        }

        public override void OnException(MethodExecutionArgs args)
        {
            Debug.WriteLine("Exeption");
        }

        public override void OnExit(MethodExecutionArgs args)
        {
            Debug.WriteLine("Exit");
        }
    }
}
