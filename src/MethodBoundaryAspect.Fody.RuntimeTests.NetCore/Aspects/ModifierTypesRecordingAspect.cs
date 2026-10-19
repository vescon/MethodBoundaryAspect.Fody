using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Aspects
{
    /// <summary>
    /// Reads the arguments and the return value, so they are boxed into MethodExecutionArgs.
    /// </summary>
    public class ModifierTypesRecordingAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnExit(MethodExecutionArgs arg)
        {
            var arguments = string.Join(", ", arg.Arguments.Select(a => a ?? "null"));
            lock (Calls)
                Calls.Add($"{arg.Method.Name}({arguments}) returned {arg.ReturnValue ?? "null"}");
        }
    }
}
