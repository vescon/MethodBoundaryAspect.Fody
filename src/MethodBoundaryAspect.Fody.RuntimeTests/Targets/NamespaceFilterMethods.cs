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

    // the type name of a generic type has the number of type parameters
    [NamespaceFilterRecordingAspect(TypeNameFilter = "^TypeNameFilterGeneric`1$")]
    public class TypeNameFilterGeneric<T>
    {
        public void Matching()
        {
        }
    }

    [NamespaceFilterRecordingAspect(TypeNameFilter = "^TypeNameFilterGenericWithoutArity$")]
    public class TypeNameFilterGenericWithoutArity<T>
    {
        public void NotMatching()
        {
        }
    }

    // the filter is a regular expression matched against the method name, accessors are named get_Name/set_Name
    [NamespaceFilterRecordingAspect(MethodNameFilter = "^(Weave|get_Weaved|set_Weaved)")]
    public class MethodNameFilterMethods
    {
        public void WeaveThis()
        {
        }

        public void SkipThis()
        {
        }

        private void WeavePrivate()
        {
        }

        public int Weaved { get; set; }

        public int Skipped { get; set; }

        public void CallPrivate() => WeavePrivate();
    }

    // not anchored: the filter matches a part of the name
    [NamespaceFilterRecordingAspect(MethodNameFilter = "Part")]
    public class MethodNameFilterNotAnchored
    {
        public void HasPartInTheMiddle()
        {
        }

        public void Other()
        {
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
