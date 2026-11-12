using System.Linq;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Mono.Cecil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// The executor method runs the original code, so it keeps the attributes which change how that code is debugged,
    /// trimmed or run. The original method (the wrapper) keeps all of its attributes.
    /// </summary>
    public class ExecutorAttributeTests
    {
        private readonly TypeDefinition _type;

        public ExecutorAttributeTests()
        {
            var weaver = new ModuleWeaver
            {
                ModuleDefinition = WeaverTestHelper.ReadModule(typeof(ExecutorAttributeMethods))
            };
            weaver.AddClassFilter(typeof(ExecutorAttributeMethods).FullName);
            weaver.Execute();
            _type = weaver.ModuleDefinition.GetType(typeof(ExecutorAttributeMethods).FullName);
        }

        [Fact]
        public void IfAMethodIsWeaved_ThenTheExecutorGetsTheDebuggerAndTrimmingAttributes()
        {
            var executor = _type.Methods.Single(m => m.Name == "$_executor_Annotated");

            executor.CustomAttributes.Select(a => a.AttributeType.Name).Should().BeEquivalentTo(
                "DebuggerHiddenAttribute", "DebuggerNonUserCodeAttribute", "RequiresUnreferencedCodeAttribute", "UnconditionalSuppressMessageAttribute");
        }

        [Fact]
        public void IfAMethodIsWeaved_ThenTheExecutorGetsTheArgumentsOfTheAttributes()
        {
            var executor = _type.Methods.Single(m => m.Name == "$_executor_Annotated");

            executor.CustomAttributes.Single(a => a.AttributeType.Name == "RequiresUnreferencedCodeAttribute")
                .ConstructorArguments.Single().Value.Should().Be("Uses reflection");
            var suppress = executor.CustomAttributes.Single(a => a.AttributeType.Name == "UnconditionalSuppressMessageAttribute");
            suppress.ConstructorArguments.Select(a => a.Value).Should().Equal("Trimming", "IL2026");
            suppress.Properties.Single().Argument.Value.Should().Be("Test");
        }

        [Fact]
        public void IfAMethodIsWeaved_ThenTheExecutorGetsTheAnnotationsOfTheGenericParameterTheParametersAndTheReturnValue()
        {
            var executor = _type.Methods.Single(m => m.Name == "$_executor_GenericAnnotated");

            Names(executor.GenericParameters.Single().CustomAttributes).Should().Equal("DynamicallyAccessedMembersAttribute");
            Names(executor.Parameters.Single().CustomAttributes).Should().Equal("DynamicallyAccessedMembersAttribute");
            Names(executor.MethodReturnType.CustomAttributes).Should().Equal("DynamicallyAccessedMembersAttribute");
            executor.GenericParameters.Single().CustomAttributes.Single().ConstructorArguments.Single().Value
                .Should().Be((int)System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors);
        }

        [Fact]
        public void IfAMethodIsWeaved_ThenTheWrapperKeepsTheAnnotationsOfTheGenericParameterTheParametersAndTheReturnValue()
        {
            var method = _type.Methods.Single(m => m.Name == nameof(ExecutorAttributeMethods.GenericAnnotated));

            Names(method.GenericParameters.Single().CustomAttributes).Should().Equal("DynamicallyAccessedMembersAttribute");
            Names(method.Parameters.Single().CustomAttributes).Should().Equal("DynamicallyAccessedMembersAttribute");
            Names(method.MethodReturnType.CustomAttributes).Should().Equal("DynamicallyAccessedMembersAttribute");
        }

        private static System.Collections.Generic.IEnumerable<string> Names(Mono.Collections.Generic.Collection<CustomAttribute> attributes) =>
            attributes.Select(a => a.AttributeType.Name);

        [Fact]
        public void IfAMethodIsWeaved_ThenTheOriginalMethodKeepsItsAttributes()
        {
            var method = _type.Methods.Single(m => m.Name == nameof(ExecutorAttributeMethods.Annotated));

            method.CustomAttributes.Select(a => a.AttributeType.Name).Should().Contain(
                new[] { "DebuggerHiddenAttribute", "RequiresUnreferencedCodeAttribute", "ObsoleteAttribute" });
        }
    }
}
