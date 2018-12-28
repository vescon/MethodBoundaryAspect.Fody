using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class SetNamedArgumentAspect : OnMethodBoundaryAspect
    {
        public int Value { get; set; }

        public bool BoolValue { get; set; }

        public AllowedValue AllowedValue { get; set; }

        public string Field;

        public int IntField;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            SetNamedArgumentAspectMethods.Result = $"Value: {Value}, BoolValue: {BoolValue}, AllowedValue: {AllowedValue}, Field: {Field}, IntField: {IntField}";
        }
    }
}