using System;
using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class FlowBehaviorClass
    {
        [FlowBehaviorAspect(Behavior = FlowBehavior.Continue, ReturnValue = "Changed")]
        public string SuppressAndReturnString()
        {
            string s = "Original";
            if (s.Length > 1)
                throw new Exception("An exception");
            return s;
        }

        [FlowBehaviorAspect(Behavior = FlowBehavior.Continue, ReturnValue = 42)]
        public int SuppressAndReturnValueType()
        {
            int i = -1;
            if (i < 0)
                throw new Exception("An exception");
            return i;
        }

        [FlowBehaviorAspect(Behavior = FlowBehavior.Default)]
        public void Default()
        {
            throw new Exception("An exception");
        }

        [FlowBehaviorAspect(Behavior=FlowBehavior.RethrowException)]
        public void Rethrow()
        {
            throw new Exception("An exception");
        }

        [FlowBehaviorAspect(Behavior =FlowBehavior.Continue)]
        public void Suppress()
        {
            throw new Exception("An exception");
        }
    }
}
