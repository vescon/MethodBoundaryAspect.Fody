using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class DerivedExternalAttribute : ExternalBaseAspect
    {
        protected override string Label => "derived";
    }

    public class ExternalAspectMethods
    {
        [ExternalStateless(Name = "a")]
        public int Stateless() => 1;

        [ExternalCounting]
        public void Counting()
        {
        }

        [ExternalGeneric<string>]
        public void Generic()
        {
        }

        [ExternalGeneric<int>]
        public void GenericOfInt()
        {
        }

        [DerivedExternal]
        public void Derived()
        {
        }
    }

    [ExternalStateless(Name = "class")]
    public class ExternalAspectOnClass
    {
        public void First()
        {
        }

        public void Second()
        {
        }
    }
}
