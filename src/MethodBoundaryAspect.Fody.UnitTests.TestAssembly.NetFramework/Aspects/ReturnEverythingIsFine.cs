using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
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
