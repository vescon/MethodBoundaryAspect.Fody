using System.Globalization;
using System.Linq;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class SetConstructorArgumentFloatingPointAspect : OnMethodBoundaryAspect
    {
        public SetConstructorArgumentFloatingPointAspect(
            double doubleValue,
            float floatValue,
            char charValue,
            double[] doubleArray,
            object boxedValue)
        {
            DoubleValue = doubleValue;
            FloatValue = floatValue;
            CharValue = charValue;
            DoubleArray = doubleArray;
            BoxedValue = boxedValue;
        }

        public double DoubleValue { get; }

        public float FloatValue { get; }

        public char CharValue { get; }

        public double[] DoubleArray { get; }

        public object BoxedValue { get; }

        public double NamedDoubleValue { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            SetConstructorArgumentFloatingPointAspectMethods.Result = string.Format(
                CultureInfo.InvariantCulture,
                "Double: {0}, Float: {1}, Char: {2}, DoubleArray: [{3}], Boxed: {4} ({5}), NamedDouble: {6}",
                DoubleValue,
                FloatValue,
                CharValue,
                string.Join(", ", DoubleArray.Select(x => x.ToString(CultureInfo.InvariantCulture))),
                BoxedValue,
                BoxedValue.GetType().Name,
                NamedDoubleValue);
        }
    }
}
