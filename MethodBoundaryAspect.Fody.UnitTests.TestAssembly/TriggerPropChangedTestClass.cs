using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class TriggerPropChangedTestClass
    {
        public static string Result { get; set; }

        [TriggerPropChangedAspect("a")]
        public string TestProp { get; set; }

        [Aspects.TriggerPropChangedAspect("expectedResult")]
        public string TestPropCrossDll { get; set; }

        [Aspects.TriggerPropChangedAspect(typeof(string), typeof(object))]
        public string TestTypeArrayCrossDll { get; set; }
    }

    public class TriggerPropChangedAspectAttribute : OnMethodBoundaryAspect
    {
        public TriggerPropChangedAspectAttribute(string property) => TriggerPropChangedTestClass.Result = property;

        public override void OnEntry(MethodExecutionArgs arg) => base.OnEntry(arg);

        public override void OnException(MethodExecutionArgs arg) => base.OnException(arg);

        public override void OnExit(MethodExecutionArgs arg) => base.OnExit(arg);
    }
}
