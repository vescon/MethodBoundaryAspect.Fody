using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class DebugScopeTests : MethodBoundaryAspectNetFrameworkTestBase
    {
        private const string TestMethodName = nameof(BlockScopedVariablesMethods.MethodWithBlockScopedVariables);
        private const string ExecutorMethodName = "$_executor_" + TestMethodName;

        [Fact]
        public void IfMethodHasBlockScopedVariables_ThenTheWeavedAssemblyShouldBeValid()
        {
            // Arrange
            var testClassType = typeof(BlockScopedVariablesMethods);

            // Act
            WeaveAssemblyClassAndLoad(testClassType);
            var result = AssemblyLoader.InvokeMethod(testClassType.TypeInfo(), TestMethodName, 4);

            // Assert
            result.Should().Be(BlockScopedVariablesMethods.MethodWithBlockScopedVariables(4));
        }

        [Fact]
        public void IfMethodHasBlockScopedVariables_ThenTheClonedMethodShouldKeepTheNestedDebugScopes()
        {
            // Arrange
            var testClassType = typeof(BlockScopedVariablesMethods);
            var expectedScopeTree = ReadScopeTree(GetDllPath(testClassType), testClassType.FullName, TestMethodName);

            // Act
            WeaveAssemblyClass(testClassType);
            var actualScopeTree = ReadScopeTree(WeavedAssemblyPath, testClassType.FullName, ExecutorMethodName);

            // Assert
            expectedScopeTree.Should().Contain(new[] { "i", "two", "three" }, "the test method must produce nested scopes");
            actualScopeTree.Should().Equal(expectedScopeTree);
        }

        [Fact]
        public void IfMethodHasBlockScopedVariables_ThenTheClonedMethodDebugInfoShouldReferenceItsOwnInstructions()
        {
            // Arrange
            var testClassType = typeof(BlockScopedVariablesMethods);

            // Act
            WeaveAssemblyClass(testClassType);

            // Assert
            using (var module = ReadModule(WeavedAssemblyPath))
            {
                var method = GetMethod(module, testClassType.FullName, ExecutorMethodName);
                var instructionOffsets = new HashSet<int>(method.Body.Instructions.Select(i => i.Offset));

                method.DebugInformation.HasSequencePoints.Should().BeTrue("breakpoints need sequence points (see #101)");
                method.DebugInformation.Scope.Scopes.Should().NotBeEmpty();

                foreach (var scope in Flatten(method.DebugInformation.Scope))
                {
                    AssertOffsetBelongsToMethod(scope.Start, instructionOffsets);
                    AssertOffsetBelongsToMethod(scope.End, instructionOffsets);
                }
            }
        }

        private static void AssertOffsetBelongsToMethod(InstructionOffset offset, HashSet<int> instructionOffsets)
        {
            if (offset.IsEndOfMethod)
                return;

            instructionOffsets.Should().Contain(offset.Offset);
        }

        private static string GetDllPath(System.Type type)
        {
            return type.Assembly.CodeBase.Replace(@"file:///", string.Empty);
        }

        /// <summary>
        /// Returns the variable names of the scope hierarchy, with a nesting marker per scope,
        /// e.g. "{", "one", "sum", "{", "i", "{", "two", ... "}", "}", "}".
        /// </summary>
        private static List<string> ReadScopeTree(string assemblyPath, string typeName, string methodName)
        {
            using (var module = ReadModule(assemblyPath))
            {
                var method = GetMethod(module, typeName, methodName);
                var result = new List<string>();
                AppendScope(method.DebugInformation.Scope, result);
                return result;
            }
        }

        private static void AppendScope(ScopeDebugInformation scope, List<string> result)
        {
            result.Add("{");
            result.AddRange(scope.Variables.Select(v => v.Name).OrderBy(n => n));
            foreach (var childScope in scope.Scopes)
                AppendScope(childScope, result);
            result.Add("}");
        }

        private static IEnumerable<ScopeDebugInformation> Flatten(ScopeDebugInformation scope)
        {
            yield return scope;
            foreach (var childScope in scope.Scopes)
            {
                foreach (var nested in Flatten(childScope))
                    yield return nested;
            }
        }

        private static ModuleDefinition ReadModule(string assemblyPath)
        {
            return ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters
            {
                ReadSymbols = true,
                SymbolReaderProvider = new PdbReaderProvider()
            });
        }

        private static MethodDefinition GetMethod(ModuleDefinition module, string typeName, string methodName)
        {
            return module.GetType(typeName).Methods.Single(m => m.Name == methodName);
        }
    }
}
