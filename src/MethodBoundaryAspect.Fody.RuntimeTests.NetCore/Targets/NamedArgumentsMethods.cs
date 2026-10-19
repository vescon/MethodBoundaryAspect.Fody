using MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Targets
{
    public class NamedArgumentsMethods
    {
        [InheritedNamedArgumentsAspect(BaseField = "base", BaseProperty = 42)]
        public void InheritedFieldAndProperty()
        {
        }

        [ArrayArgumentsAspect(new[] { "x", "y" }, typeof(string),
            Tags = new[] { "a", "b" },
            Numbers = new[] { 1, 2, 3 },
            Kinds = new[] { ArrayArgumentKind.Second, ArrayArgumentKind.First },
            Types = new[] { typeof(int), typeof(string) },
            FieldTags = new[] { "c" })]
        public void ArrayArguments()
        {
        }

        [ArrayArgumentsAspect(null, null, Tags = null, FieldTags = null)]
        public void NullArrayArguments()
        {
        }

        [ArrayArgumentsAspect(Tags = new string[0])]
        public void EmptyArrayArgument()
        {
        }
    }
}
