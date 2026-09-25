using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// Weaves a .NET Framework assembly while the weaver itself runs on .NET (as Fody does under
    /// Core MSBuild / 'dotnet build'). The woven IL must only reference the target's core library
    /// (mscorlib), never the host's System.Private.CoreLib.
    /// </summary>
    public class NetFrameworkTargetWeavingTests : IClassFixture<NetFrameworkTargetWeavingTests.WeavedNetFrameworkAssembly>
    {
        private const string TargetCoreLibrary = "mscorlib";
        private const string HostCoreLibrary = "System.Private.CoreLib";

        private readonly ModuleDefinition _weavedModule;

        public NetFrameworkTargetWeavingTests(WeavedNetFrameworkAssembly weavedAssembly)
        {
            _weavedModule = weavedAssembly.Module;
        }

        [Fact]
        public void IfNetFrameworkAssemblyIsWeavedOnNetCoreHost_ThenNoTypeOrMemberReferencesHostCoreLibrary()
        {
            var hostCoreLibraryReferences = _weavedModule.GetTypeReferences()
                .Where(t => t.Scope.Name == HostCoreLibrary)
                .Select(t => t.FullName)
                .Concat(_weavedModule.GetMemberReferences()
                    .Where(m => m.DeclaringType.Scope.Name == HostCoreLibrary)
                    .Select(m => m.FullName));

            hostCoreLibraryReferences.Should().BeEmpty();
        }

        [Fact]
        public void IfNetFrameworkAssemblyIsWeavedOnNetCoreHost_ThenHostCoreLibraryIsNotAnAssemblyReference()
        {
            _weavedModule.AssemblyReferences.Select(r => r.Name).Should().NotContain(HostCoreLibrary);
        }

        [Fact]
        public void IfNetFrameworkAssemblyIsWeavedOnNetCoreHost_ThenGetMethodFromHandleIsResolvedFromTargetCoreLibrary()
        {
            var getMethodFromHandleCalls = GetCalledMethods()
                .Where(m => m.Name == "GetMethodFromHandle")
                .ToList();

            // both overloads are emitted: 1 arg in the MethodInfos cache, 2 args for open generic types
            getMethodFromHandleCalls.Select(m => m.Parameters.Count).Distinct().Should().BeEquivalentTo(new[] { 1, 2 });
            getMethodFromHandleCalls.Should().OnlyContain(m =>
                m.DeclaringType.Scope.Name == TargetCoreLibrary
                && m.ReturnType.Scope.Name == TargetCoreLibrary
                && m.Parameters.All(p => p.ParameterType.Scope.Name == TargetCoreLibrary));
        }

        [Fact]
        public void IfNetFrameworkAssemblyIsWeavedOnNetCoreHost_ThenMethodInfosCacheFieldsUseTargetCoreLibrary()
        {
            var methodInfos = _weavedModule.GetType("OnMethodBoundaryAspectCompile", "MethodInfos");

            methodInfos.Should().NotBeNull();
            methodInfos.Fields.Should().NotBeEmpty();
            methodInfos.Fields.Should().OnlyContain(f => f.FieldType.Scope.Name == TargetCoreLibrary);
        }

        private IEnumerable<MethodReference> GetCalledMethods()
        {
            return _weavedModule.GetTypes()
                .SelectMany(t => t.Methods)
                .Where(m => m.HasBody)
                .SelectMany(m => m.Body.Instructions)
                .Where(i => i.OpCode == OpCodes.Call)
                .Select(i => i.Operand)
                .OfType<MethodReference>();
        }

        public class WeavedNetFrameworkAssembly : IDisposable
        {
            private static readonly string NetFrameworkFolder = Path.Combine(AppContext.BaseDirectory, "NetFramework");

            private static readonly string ReferenceAssembliesFolder = Environment.ExpandEnvironmentVariables(
                @"%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.2");

            private readonly string _weavedAssemblyPath;

            public WeavedNetFrameworkAssembly()
            {
                var assemblyPath = Path.Combine(NetFrameworkFolder, "MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.dll");
                var assemblyResolver = new FolderAssemblyResolver(
                    new DefaultAssemblyResolver(),
                    ReferenceAssembliesFolder,
                    Path.Combine(ReferenceAssembliesFolder, "Facades"),
                    NetFrameworkFolder);

                _weavedAssemblyPath = new ModuleWeaver().WeaveToShadowFile(assemblyPath, assemblyResolver);
                Module = ModuleDefinition.ReadModule(_weavedAssemblyPath);
            }

            public ModuleDefinition Module { get; }

            public void Dispose()
            {
                Module.Dispose();
                File.Delete(_weavedAssemblyPath);
                File.Delete(Path.ChangeExtension(_weavedAssemblyPath, "pdb"));
            }
        }
    }
}
