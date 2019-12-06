using System;
using System.Diagnostics;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.MultipleAspects
{
    [ProvideAspectRole(TestRoles.Second)]
#pragma warning disable 0618
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, TestRoles.First)]
#pragma warning restore 0618
    public class SecondAspect : OnMethodBoundaryAspect
    {
        private const string MethodExecutionTagValue = TestRoles.Second;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Debug.WriteLine("SecondAspect - OnEntry called for: " + arg.Method.Name);
            arg.MethodExecutionTag = MethodExecutionTagValue;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            Debug.WriteLine("SecondAspect - OnExit called for: " + arg.Method.Name);
            var value = (string)arg.MethodExecutionTag;
            Debug.WriteLine("SecondAspect - MethodExecutionTag is: " + value);
            if (value != MethodExecutionTagValue)
                throw new InvalidOperationException("SecondAspect - MethodExecutionTag was changed outside of aspect");
        }
    }
}