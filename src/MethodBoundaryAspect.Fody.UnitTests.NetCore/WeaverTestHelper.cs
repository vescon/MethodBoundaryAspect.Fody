using MethodBoundaryAspect.Fody.UnitTests.Shared;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// What the tests of the weaver share: reading the modules of the test assemblies, capturing the warnings of the
    /// weaver and cleaning up the woven shadow files.
    /// </summary>
    internal static class WeaverTestHelper
    {
        public static ModuleDefinition ReadModule(Type type) => ReadModule(type.Assembly);

        public static ModuleDefinition ReadModule(Assembly assembly) =>
            ModuleDefinition.ReadModule(assembly.Location, new ReaderParameters { AssemblyResolver = ModuleHelper.AssemblyResolver });

        /// <summary>
        /// Reads a woven assembly from memory, so that the file can be deleted while the module is in use.
        /// </summary>
        public static ModuleDefinition ReadWeavedModule(string weavedAssemblyPath) =>
            ModuleDefinition.ReadModule(new MemoryStream(File.ReadAllBytes(weavedAssemblyPath)),
                new ReaderParameters { AssemblyResolver = ModuleHelper.AssemblyResolver });

        public static TypeDefinition ResolveType(Type type) => ReadModule(type).GetType(type.FullName);

        public static void DeleteWeavedAssembly(string weavedAssemblyPath)
        {
            File.Delete(weavedAssemblyPath);
            File.Delete(Path.ChangeExtension(weavedAssemblyPath, "pdb"));
        }

        /// <summary>
        /// Collects the warnings of the weaver. Fody sets these (for consumers obsolete) callbacks, they are the only
        /// way to capture the warnings.
        /// </summary>
        public static ModuleWeaver CaptureWarnings(this ModuleWeaver weaver, ICollection<string> warnings)
        {
#pragma warning disable CS0618
            weaver.LogWarning = warnings.Add;
            weaver.LogWarningPoint = (message, _) => warnings.Add(message);
#pragma warning restore CS0618
            return weaver;
        }
    }
}
