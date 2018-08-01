using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class AsyncOverwrite
    {
        public static string TestReturnArg(string arg) => ReturnArg(arg).Result;

        [ReturnEverythingIsFine]
        public static Task<string> ReturnArg(string arg) => Task.FromResult(arg);
    }
}
