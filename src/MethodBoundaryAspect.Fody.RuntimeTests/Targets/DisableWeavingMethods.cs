using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    [NameRecordingAspect]
    public class DisableWeavingOnPropertyMethods
    {
        [DisableWeaving]
        public int Disabled { get; set; }

        public int Enabled { get; set; }

        // [DisableWeaving] on the property wins over an aspect applied to the property
        [NameRecordingAspect]
        [DisableWeaving]
        public int DisabledWithOwnAspect { get; set; }

        public int DisabledGetterOnly { [DisableWeaving] get; set; }
    }
}
