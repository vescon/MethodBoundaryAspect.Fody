using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Aspects
{
    public class EmptyMethodBoundaryAspect : OnMethodBoundaryAspect
	{
		public override void OnEntry(MethodExecutionArgs arg)
		{
		}

		public override void OnException(MethodExecutionArgs arg)
		{
		}

		public override void OnExit(MethodExecutionArgs arg)
		{
		}
	}
}
