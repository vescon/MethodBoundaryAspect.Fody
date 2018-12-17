using System;
using MethodBoundaryAspect.Fody.Attributes;

namespace HelloWorld_NetFramework_Attributes
{
    public sealed class HelloWorldAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            Console.WriteLine("Entered method: " + arg.Method.Name);
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            Console.WriteLine("Exited method: " + arg.Method.Name);
        }

        public override void OnException(MethodExecutionArgs args)
        {
            Console.WriteLine("Exception: " + args.Exception.Message);
        }
    }
}