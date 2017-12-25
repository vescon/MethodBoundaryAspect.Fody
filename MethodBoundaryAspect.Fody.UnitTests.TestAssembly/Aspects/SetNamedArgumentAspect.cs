using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public class SetNamedArgumentAspect : OnMethodBoundaryAspect
    {
        public int Value { get; set; }

        public bool BoolValue { get; set; }

        public AllowedValue AllowedValue { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            SetNamedArgumentAspectMethods.Result =
                string.Format("Value: {0}, BoolValue: {1}, AllowedValue: {2}",
                    Value,
                    BoolValue,
                    AllowedValue);
        }
    }
}