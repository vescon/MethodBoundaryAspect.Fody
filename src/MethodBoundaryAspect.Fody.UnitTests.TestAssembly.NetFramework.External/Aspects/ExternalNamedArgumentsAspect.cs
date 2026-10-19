using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.External.Aspects
{
    /// <summary>
    /// An aspect of another assembly with a field set as named argument: the field has to be imported.
    /// </summary>
    public class ExternalNamedArgumentsAspect : OnMethodBoundaryAspect
    {
        public static string LastResult;

        public string Field;

        public string[] Tags { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            LastResult = $"Field: {Field}, Tags: {(Tags == null ? "null" : string.Join(", ", Tags))}";
        }
    }
}
