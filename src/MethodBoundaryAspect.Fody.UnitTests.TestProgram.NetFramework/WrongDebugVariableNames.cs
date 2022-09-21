using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.UnitTests.TestProgram.NetFramework
{
    internal class WrongDebugVariableNames
    {
		public void Call()
        {
			DoSomeStuff2(new List<int>{42});
        }

        [MethodInterceptor]
		private void DoSomeStuff2(List<int> youcanseethis)
        {
			var andThis = youcanseethis.Take(1);
			var rand = new Random();
			if (rand.NextDouble() > 0.5)
            {
				var butNotThis = youcanseethis.Take(1);
				Console.WriteLine("Larger than 0.5");
				Console.WriteLine($"{butNotThis}");
            }
			else
            {
				var butNotThis = youcanseethis.Take(1);
				Console.WriteLine("Smaller than 0.5");
				Console.WriteLine($"{butNotThis}");
			}
        }
    }

	public sealed class MethodInterceptor : OnMethodBoundaryAspect
	{
		public override void OnEntry(MethodExecutionArgs args)
		{
			Console.WriteLine("Enter: " + args.Instance.GetType().FullName + "." + args.Method.Name);
		}

		public override void OnExit(MethodExecutionArgs args)
		{
			Console.WriteLine("Exit: "+ args.Instance.GetType().FullName +"."+args.Method.Name);
		}

		public override void OnException(MethodExecutionArgs args)
		{
			Console.WriteLine(args.Exception);
		}
	}
}
