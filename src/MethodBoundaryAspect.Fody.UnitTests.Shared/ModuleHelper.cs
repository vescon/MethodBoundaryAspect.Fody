using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Mono.Cecil;

namespace MethodBoundaryAspect.Fody.UnitTests.Shared
{
    // excerpt of https://github.com/tom-englert/FodyTools/blob/master/FodyTools.Tests/Tools/ModuleHelper.cs
    public static class ModuleHelper
    {
        public static FrameworkName GetTargetFrameworkName(this Type typeInTargetAssembly)
        {
            return typeInTargetAssembly.Assembly
                .CustomAttributes
                .Where(attr => attr.AttributeType.FullName == typeof(TargetFrameworkAttribute).FullName)
                .Select(attr => attr.ConstructorArguments.Select(arg => arg.Value as string).FirstOrDefault())
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => new FrameworkName(name))
                .FirstOrDefault();
        }

        public static IAssemblyResolver AssemblyResolver => new AssemblyResolverAdapter(typeof(ModuleHelper).GetTargetFrameworkName());

        private interface IInternalAssemblyResolver
        {

            AssemblyDefinition Resolve(AssemblyNameReference nameReference, ReaderParameters parameters);
        }

        private class AssemblyResolverAdapter : IAssemblyResolver
        {
            private readonly Dictionary<string, AssemblyDefinition> _cache = new Dictionary<string, AssemblyDefinition>();
            private readonly IAssemblyResolver _defaultResolver = new DefaultAssemblyResolver();

            private IInternalAssemblyResolver _internalResolver;

            public AssemblyResolverAdapter()
            {
            }

            public AssemblyResolverAdapter(FrameworkName frameworkName)
            {
                Init(frameworkName);
            }

            public void Init(FrameworkName frameworkName)
            {
                switch (frameworkName?.Identifier)
                {
                    case ".NETFramework":
                        _internalResolver = new NetFrameworkAssemblyResolver(frameworkName.Version);
                        break;
                }
            }


            public AssemblyDefinition Resolve(AssemblyNameReference nameReference)
            {
                var name = nameReference.Name;

                if (_cache.TryGetValue(name, out var definition))
                    return definition;

                var assemblyDefinition = Resolve(nameReference, new ReaderParameters { AssemblyResolver = this });

                _cache.Add(name, assemblyDefinition);

                return assemblyDefinition;
            }


            public AssemblyDefinition Resolve(AssemblyNameReference nameReference, ReaderParameters parameters)
            {
                return _internalResolver?.Resolve(nameReference, parameters) ?? _defaultResolver.Resolve(nameReference, parameters);
            }

            public void Dispose()
            {
                _defaultResolver.Dispose();
            }
        }

        private class NetFrameworkAssemblyResolver : IInternalAssemblyResolver
        {
            private readonly string _refAssembliesFolder;

            public NetFrameworkAssemblyResolver(Version frameworkVersion)
            {
                _refAssembliesFolder = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v" + frameworkVersion);
            }


            public AssemblyDefinition Resolve(AssemblyNameReference nameReference, ReaderParameters parameters)
            {
                var name = nameReference.Name;

                var path = Path.Combine(_refAssembliesFolder, name + ".dll");
                if (!File.Exists(path))
                    return null;

                var assemblyDefinition = AssemblyDefinition.ReadAssembly(path, parameters);

                return assemblyDefinition;
            }
        }
    }
}
