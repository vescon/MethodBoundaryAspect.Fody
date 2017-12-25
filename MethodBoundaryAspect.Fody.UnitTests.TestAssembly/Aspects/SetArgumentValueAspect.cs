using System.Linq;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public class SetArgumentValueAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            if (arg.Arguments == null)
            {
                SetArgumentValueAspectMethods.Result = "arguments was null";
                return;
            }

            SetArgumentValueAspectMethods.Result = string.Format("{0}: '{1}'",
                arg.Method.GetParameters().First().Name,
                arg.Arguments.First());
        }
    }
}