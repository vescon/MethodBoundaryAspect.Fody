using System;
using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects
{
    // Aspects for the analysis which MethodExecutionArgs properties are used, see ExecutionArgsUsageAnalyzer

    public class NoUsageAspect : OnMethodBoundaryAspect
    {
        public static int Calls;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Calls++;
        }
    }

    /// <summary>
    /// Doesn't use MethodExecutionArgs, but the optimization is disabled for it in the configuration.
    /// </summary>
    public class OptOutUsageAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
        }
    }

    public class MethodUsageAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = DateTime.Now;
            Calls.Add(arg.Method.Name);
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            Calls.Add($"{arg.Method.Name} started {arg.MethodExecutionTag}");
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            Calls.Add($"{arg.Method.Name} failed {arg.Exception.Message}");
        }
    }

    public class ArgumentsUsageAspect : OnMethodBoundaryAspect
    {
        public static int ArgumentCount;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            ArgumentCount = arg.Arguments.Length;
        }
    }

    public class ReadReturnValueUsageAspect : OnMethodBoundaryAspect
    {
        public static object ReturnValue;

        public override void OnExit(MethodExecutionArgs arg)
        {
            ReturnValue = arg.ReturnValue;
        }
    }

    public class WriteReturnValueUsageAspect : OnMethodBoundaryAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            arg.ReturnValue = 42;
        }
    }

    public class InstanceUsageAspect : OnMethodBoundaryAspect
    {
        public static object Instance;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Instance = arg.Instance;
        }
    }

    public class FlowBehaviorUsageAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.FlowBehavior = FlowBehavior.Return;
        }
    }

    /// <summary>
    /// Follows base calls: base.OnEntry reads the method, this aspect the arguments.
    /// </summary>
    public class BaseCallUsageAspect : MethodUsageAspect
    {
        public static int ArgumentCount;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            base.OnEntry(arg);
            ArgumentCount = arg.Arguments.Length;
        }
    }

    /// <summary>
    /// Follows calls to non virtual helper methods of the aspect.
    /// </summary>
    public class HelperUsageAspect : OnMethodBoundaryAspect
    {
        public static string Description;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Description = Describe(arg) + Format(1, arg);
        }

        private static string Describe(MethodExecutionArgs arg) => arg.Method.Name;

        private string Format(int count, MethodExecutionArgs arg) => $"{count} {arg.Instance}";
    }

    public class RecursiveHelperUsageAspect : OnMethodBoundaryAspect
    {
        public static int Depth;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Depth = Recurse(arg, 3);
        }

        private static int Recurse(MethodExecutionArgs arg, int depth) =>
            depth == 0 ? arg.Arguments.Length : Recurse(arg, depth - 1);
    }

    // the MethodExecutionArgs instance escapes, all properties are assumed to be used

    public class ExternalMethodUsageAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            ExecutionArgsLogger.Log(arg);
        }
    }

    public class VirtualHelperUsageAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            Log(arg);
        }

        protected virtual void Log(MethodExecutionArgs arg)
        {
        }
    }

    public class LambdaUsageAspect : OnMethodBoundaryAspect
    {
        public static Func<string> Describe;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Describe = () => arg.Method.Name;
        }
    }

    public class FieldUsageAspect : OnMethodBoundaryAspect
    {
        public static MethodExecutionArgs LastArgs;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            LastArgs = arg;
        }
    }

    public class TagUsageAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.MethodExecutionTag = arg;
        }
    }

    public class ToStringUsageAspect : OnMethodBoundaryAspect
    {
        public static string Text;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Text = arg.ToString();
        }
    }

    public class NullConditionalUsageAspect : OnMethodBoundaryAspect
    {
        public static string Name;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Name = arg?.Method.Name;
        }
    }

    public class ReassignedParameterUsageAspect : OnMethodBoundaryAspect
    {
        public static MethodExecutionArgs Other = new MethodExecutionArgs();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg = Other;
            arg.ReturnValue = 1;
        }
    }

    public static class ExecutionArgsLogger
    {
        public static void Log(MethodExecutionArgs arg)
        {
            Console.WriteLine(arg.Arguments.Length);
        }
    }
}
