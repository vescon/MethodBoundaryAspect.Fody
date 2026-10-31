using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects
{
    /// <summary>
    /// Sets filters in its constructor, which the weaver never sees.
    /// </summary>
    public class ConstructorFilterAspect : OnMethodBoundaryAspect
    {
        public ConstructorFilterAspect()
        {
            NamespaceFilter = "^Somewhere";
            AttributeTargetMemberAttributes = MulticastAttributes.Public;
        }

        public override void OnEntry(MethodExecutionArgs arg)
        {
        }
    }

    /// <summary>
    /// The filter is inherited from the base aspect.
    /// </summary>
    public class DerivedConstructorFilterAspect : ConstructorFilterAspect
    {
    }

    /// <summary>
    /// Overrides a filter property, which the weaver never sees.
    /// </summary>
    public class OverridingFilterAspect : OnMethodBoundaryAspect
    {
        public override string TypeNameFilter { get; set; } = "Service$";

        public override void OnEntry(MethodExecutionArgs arg)
        {
        }
    }

    /// <summary>
    /// Generic aspect: the name in the warning must be the plain type name.
    /// </summary>
    public class GenericConstructorFilterAspect<T> : OnMethodBoundaryAspect
    {
        public GenericConstructorFilterAspect()
        {
            NamespaceFilter = "^Somewhere";
        }

        public override void OnEntry(MethodExecutionArgs arg)
        {
        }
    }
}
