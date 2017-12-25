using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public class SetConstructorArgumentPrimitivesAspect : OnMethodBoundaryAspect
    {
        public SetConstructorArgumentPrimitivesAspect(int value, AllowedValue allowedValue)
        {
            Value = value;
            AllowedValue = allowedValue;
        }

        public int Value { get; set; }
        
        public AllowedValue AllowedValue { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            SetConstructorArgumentPrimitivesAspectMethods.Result =
                string.Format("Value: {0}, AllowedValue: {1}",
                    Value,
                    AllowedValue);
        }
    }
}