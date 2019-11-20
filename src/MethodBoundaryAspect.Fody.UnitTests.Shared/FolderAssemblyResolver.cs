using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace MethodBoundaryAspect.Fody.UnitTests.Shared
{
    public class FolderAssemblyResolver : IAssemblyResolver
    {
        private readonly IAssemblyResolver _defaultAssemblyResolver;

        public FolderAssemblyResolver(IAssemblyResolver defaultAssemblyResolver, params string[] folders)
        {
            Folders = folders;
            _defaultAssemblyResolver = defaultAssemblyResolver;
        }

        public IList<string> Folders { get; }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            var assemblyPath = Folders
                .SelectMany(x => Directory.GetFiles(x, name.Name + ".dll"))
                .SingleOrDefault();

            if (assemblyPath != null)
                return AssemblyDefinition.ReadAssembly(assemblyPath);

            return _defaultAssemblyResolver.Resolve(name);
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            throw new NotSupportedException();
        }

        public AssemblyDefinition Resolve(string fullName)
        {
            throw new NotSupportedException();
        }

        public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
        {
            throw new NotSupportedException();
        }

		public void Dispose()
		{
			_defaultAssemblyResolver.Dispose();
		}
	}
}