using System;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class SetConstructorArgumentReferencesAspect : OnMethodBoundaryAspect
    {
        public SetConstructorArgumentReferencesAspect(Type type, string text)
        {
            Type = type;
            Text = text;
        }

        public Type Type { get; set; }
        public string Text { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            SetConstructorArgumentReferencesAspectMethods.Result =
                string.Format("Type: {0}, Text: {1}",
                    Type,
                    Text);
        }
    }
}