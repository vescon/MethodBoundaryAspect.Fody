using MethodBoundaryAspect.Fody.RuntimeTests.AssemblyLevel;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

// an aspect of the assembly is woven into all types, narrowed down here to the namespace of the test classes
[assembly: AssemblyLevel("assembly", NamespaceFilter = @"^MethodBoundaryAspect\.Fody\.RuntimeTests\.AssemblyLevel$")]

namespace MethodBoundaryAspect.Fody.RuntimeTests.AssemblyLevel
{
    public class OnlyTheAssemblyAspect
    {
        public void Method()
        {
        }

        public int Property { get; set; }

        public class Nested
        {
            public void NestedMethod()
            {
            }
        }
    }

    [AssemblyLevel("class")]
    public class AssemblyAndClassAspect
    {
        [AssemblyLevel("method")]
        public void Both()
        {
        }

        public void ClassOnly()
        {
        }
    }

    // not woven by the assembly aspect, it is excluded by the type name
    [MethodBoundaryAspect.Fody.Attributes.DisableWeaving]
    public class ExcludedByAttribute
    {
        public void Method()
        {
        }
    }
}

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    // outside of the namespace of the filter
    public class OutsideTheAssemblyAspectNamespace
    {
        public void Method()
        {
        }
    }
}
