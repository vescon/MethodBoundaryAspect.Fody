using System;
using System.Reflection;
using Mono.Cecil;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// Imports reflection types of the weaver host's core library into the target module's core library.
    /// The default importer scopes them to the host (System.Private.CoreLib when Fody runs under Core MSBuild)
    /// and adds that assembly reference to the module, even if the type is rescoped afterwards.
    /// </summary>
    public class CoreLibraryReflectionImporter : DefaultReflectionImporter
    {
        private static readonly Assembly HostCoreLibrary = typeof(object).Assembly;

        private readonly ModuleDefinition _module;

        public CoreLibraryReflectionImporter(ModuleDefinition module)
            : base(module)
        {
            _module = module;
        }

        protected override IMetadataScope ImportScope(Type type)
        {
            return type.Assembly == HostCoreLibrary
                ? _module.TypeSystem.CoreLibrary
                : base.ImportScope(type);
        }
    }
}
