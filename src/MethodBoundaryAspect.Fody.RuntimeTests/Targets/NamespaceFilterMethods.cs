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
