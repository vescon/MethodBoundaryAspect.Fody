using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssemblyDelegateFwk
{
    public sealed class WeaveAllMethodsInAssemblyAttribute : OnMethodBoundaryAspect
    {
        public override void OnException(MethodExecutionArgs args)
        {
                args.FlowBehavior = FlowBehavior.RethrowException;
        }
    }
}
