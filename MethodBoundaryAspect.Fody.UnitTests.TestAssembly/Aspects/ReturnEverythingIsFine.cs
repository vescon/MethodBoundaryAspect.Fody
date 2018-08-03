using MethodBoundaryAspect.Fody.Attributes;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public class ReturnEverythingIsFine : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            if (arg.ReturnValue is Task<string> task)
            {
                arg.ReturnValue = task.ContinueWith(t =>
                {
                    return "Everything is fine";
                });
            }
        }
    }
}
