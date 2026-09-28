using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// MethodExecutionArgs.Method is loaded from a static field of OnMethodBoundaryAspectCompile.MethodInfos
    /// instead of being resolved by reflection on every call, also for methods of generic types and generic methods.
    /// see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/85
    /// </summary>
    [Collection(RefStructWarningTests.TestAssemblyNetCoreWeaving)]
    public class MethodInfoCacheTests : IDisposable
    {
        private static readonly string[] ReflectionMethods = { "GetCurrentMethod", "GetMethodFromHandle" };

        private readonly List<string> _weavedAssemblyPaths = new List<string>();

        [Theory]
        [InlineData(typeof(MethodInfoCacheMethods), nameof(MethodInfoCacheMethods.NonGeneric))]
        [InlineData(typeof(MethodInfoCacheMethods), nameof(MethodInfoCacheMethods.GenericMethod))]
        [InlineData(typeof(MethodInfoCacheGeneric<>), nameof(MethodInfoCacheGeneric<int>.NonGenericMethod))]
        [InlineData(typeof(MethodInfoCacheGeneric<>), nameof(MethodInfoCacheGeneric<int>.GenericMethod))]
        public void IfMethodIsWeaved_ThenTheMethodInfoIsLoadedFromTheCache(Type type, string methodName)
        {
            var weavedModule = Weave();

            var instructions = weavedModule.GetType(type.FullName).Methods.Single(m => m.Name == methodName).Body.Instructions;

            instructions.Select(i => i.Operand).OfType<MethodReference>().Select(m => m.Name)
                .Should().NotContain(ReflectionMethods);
            instructions.Where(i => i.OpCode == OpCodes.Ldsfld).Select(i => ((FieldReference)i.Operand).DeclaringType.FullName)
                .Should().ContainSingle().Which.Should().Be("OnMethodBoundaryAspectCompile.MethodInfos");
        }

        [Fact]
        public void IfMethodsAreWeaved_ThenTheCacheResolvesMethodsOfGenericTypesWithTheirDeclaringType()
        {
            var weavedModule = Weave();

            var cctor = weavedModule.GetType("OnMethodBoundaryAspectCompile", "MethodInfos").Methods.Single(m => m.IsConstructor && m.IsStatic);
            var getMethodFromHandleCalls = cctor.Body.Instructions
                .Where(i => i.OpCode == OpCodes.Call)
                .Select(i => (MethodReference)i.Operand)
                .Where(m => m.Name == "GetMethodFromHandle")
                .ToList();

            // NonGeneric uses the overload without declaring type, the others need it
            getMethodFromHandleCalls.Select(m => m.Parameters.Count).Should().BeEquivalentTo(new[] { 1, 2, 2, 2 });
        }

        private ModuleDefinition Weave()
        {
            var weaver = new ModuleWeaver();
            weaver.AddClassFilter(typeof(MethodInfoCacheMethods).FullName);
            weaver.AddClassFilter(typeof(MethodInfoCacheGeneric<>).FullName);

            var assemblyPath = typeof(MethodInfoCacheMethods).Assembly.Location;
            var weavedAssemblyPath = weaver.WeaveToShadowFile(assemblyPath, ModuleHelper.AssemblyResolver);
            _weavedAssemblyPaths.Add(weavedAssemblyPath);

            return ModuleDefinition.ReadModule(new MemoryStream(File.ReadAllBytes(weavedAssemblyPath)),
                new ReaderParameters { AssemblyResolver = ModuleHelper.AssemblyResolver });
        }

        public void Dispose()
        {
            foreach (var path in _weavedAssemblyPaths)
            {
                File.Delete(path);
                File.Delete(Path.ChangeExtension(path, "pdb"));
            }
        }
    }
}
