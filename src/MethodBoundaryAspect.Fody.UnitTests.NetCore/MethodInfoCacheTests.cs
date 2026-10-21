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
    /// Each woven method has its own getter, which loads the method info on its first call: a static constructor
    /// loaded all method infos at once, and one which couldn't be loaded broke all woven methods.
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

            var calledMethods = instructions.Select(i => i.Operand).OfType<MethodReference>().ToList();
            calledMethods.Select(m => m.Name).Should().NotContain(ReflectionMethods);
            calledMethods.Where(m => m.DeclaringType.FullName == "OnMethodBoundaryAspectCompile.MethodInfos")
                .Should().ContainSingle().Which.Name.Should().StartWith("GetMethodInfo_");
        }

        [Fact]
        public void IfMethodsAreWeaved_ThenTheCacheResolvesMethodsOfGenericTypesWithTheirDeclaringType()
        {
            var weavedModule = Weave();

            var getMethodFromHandleCalls = weavedModule.GetType("OnMethodBoundaryAspectCompile", "MethodInfos").Methods
                .SelectMany(m => m.Body.Instructions)
                .Where(i => i.OpCode == OpCodes.Call)
                .Select(i => (MethodReference)i.Operand)
                .Where(m => m.Name == "GetMethodFromHandle")
                .ToList();

            // NonGeneric uses the overload without declaring type, the others need it
            getMethodFromHandleCalls.Select(m => m.Parameters.Count).Should().BeEquivalentTo(new[] { 1, 2, 2, 2 });
        }

        [Fact]
        public void IfMethodsAreWeaved_ThenEachMethodInfoIsLoadedOnItsFirstUse()
        {
            var weavedModule = Weave();

            var methodInfos = weavedModule.GetType("OnMethodBoundaryAspectCompile", "MethodInfos");

            methodInfos.Methods.Should().NotContain(m => m.IsConstructor);
            methodInfos.Methods.Should().HaveCount(4).And.OnlyContain(m => m.Name.StartsWith("GetMethodInfo_") && m.IsAssembly && m.IsStatic);
            methodInfos.Fields.Should().HaveCount(4).And.OnlyContain(f => f.IsPrivate && f.IsStatic && !f.IsInitOnly);
        }

        [Fact]
        public void IfMethodsAreWeaved_ThenTheCacheIsNotPartOfThePublicApi()
        {
            var weavedModule = Weave();

            var methodInfos = weavedModule.GetType("OnMethodBoundaryAspectCompile", "MethodInfos");

            methodInfos.IsNotPublic.Should().BeTrue();
        }

        private ModuleDefinition Weave()
        {
            var weaver = new ModuleWeaver();
            weaver.AddClassFilter(typeof(MethodInfoCacheMethods).FullName);
            weaver.AddClassFilter(typeof(MethodInfoCacheGeneric<>).FullName);

            var assemblyPath = typeof(MethodInfoCacheMethods).Assembly.Location;
            var weavedAssemblyPath = weaver.WeaveToShadowFile(assemblyPath, ModuleHelper.AssemblyResolver);
            _weavedAssemblyPaths.Add(weavedAssemblyPath);

            return WeaverTestHelper.ReadWeavedModule(weavedAssemblyPath);
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
