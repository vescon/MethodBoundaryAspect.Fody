using MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Aspects;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Targets
{
    [EmptyMethodBoundaryAspect]
	public class AsyncEmptyMethodBoundaryAspectMethods
	{
		public async Task IfEmptyMethodBoundaryAspectMethod(bool test)
		{
			if(test)
			{
				await Task.CompletedTask;
			}
		}

		public async Task IfElseEmptyMethodBoundaryAspectMethod(bool test)
		{
			if (test)
			{
				await Task.CompletedTask;
			}
			else
			{
				await Task.CompletedTask;
			}
		}

		public async Task TryCatchMethodBoundaryAspectMethod()
		{
			try
			{
				await Task.CompletedTask;
			}
			catch
			{
			}
		}
	}
}
