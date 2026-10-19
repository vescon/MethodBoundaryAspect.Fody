using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    [NamespaceFilterRecordingAspect(NamespaceFilter = @"^MethodBoundaryAspect\.Fody\.RuntimeTests\.Targets$")]
    public class NamespaceFilterOuter
    {
        public void Outer()
        {
        }

        public class Nested
        {
            public void Inner()
            {
            }

            public class Deeper
            {
                public void Deepest()
                {
                }
            }
        }
    }

    [NamespaceFilterRecordingAspect(NamespaceFilter = "^Other$")]
    public class NamespaceFilterNotMatching
    {
        public void Outer()
        {
        }

        public class Nested
        {
            public void Inner()
            {
            }
        }
    }
}
