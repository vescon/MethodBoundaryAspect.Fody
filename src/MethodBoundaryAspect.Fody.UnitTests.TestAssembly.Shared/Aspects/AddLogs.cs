using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Shared.Aspects
{
    public interface IResult
    {
        object InstanceResult { set; }
    }

    public class AddLogs : OnMethodBoundaryAspect
    {
        static AddLogs()
        {
            Result = new List<string>();
        }

        public static List<string> Result { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            if (arg.Arguments != null && arg.Arguments.Length != 0)
                Result.Add($"Entry: {arg.Instance.GetType()} {arg.Method.Name} {arg.Arguments[0].GetType()}");
            else
                Result.Add($"Entry: {arg.Instance.GetType()} {arg.Method.Name}");
            ((IResult)arg.Instance).InstanceResult = Result;
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            Result.Add($"Exception: {arg.Instance.GetType()} {arg.Method.Name} {arg.Exception.ToString()}");
            ((IResult)arg.Instance).InstanceResult = Result;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            if (arg.ReturnValue != null)
                Result.Add($"Exit: {arg.Instance.GetType()} {arg.Method.Name} {arg.ReturnValue.GetType()}");
            else
                Result.Add($"Exit: {arg.Instance.GetType()} {arg.Method.Name}");
            ((IResult)arg.Instance).InstanceResult = Result;
        }
    }
}
