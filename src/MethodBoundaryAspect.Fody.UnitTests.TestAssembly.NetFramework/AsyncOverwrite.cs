using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class AsyncOverwrite
    {
        public static string TestReturnArg(string arg) => ReturnArg(arg).Result;

        [ReturnEverythingIsFine]
        public static Task<string> ReturnArg(string arg) => Task.FromResult(arg);
    }
}
