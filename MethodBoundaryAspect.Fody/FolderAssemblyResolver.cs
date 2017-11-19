using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace MethodBoundaryAspect.Fody
{
    public class FolderAssemblyResolver : IAssemblyResolver
    {
        private readonly IAssemblyResolver _defaultAssemblyResolver = new DefaultAssemblyResolver();

        public FolderAssemblyResolver(List<string> folder)
        {
            Folder = folder;
        }

        public List<string> Folder { get; private set; }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            var assemblyPath = Folder
                .SelectMany(x => Directory.GetFiles(x, name.Name + ".dll"))
                .SingleOrDefault();

            if (assemblyPath != null)
                return AssemblyDefinition.ReadAssembly(assemblyPath);

            return _defaultAssemblyResolver.Resolve(name);
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            throw new NotImplementedException();
        }

        public AssemblyDefinition Resolve(string fullName)
        {
            throw new NotImplementedException();
        }

        public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
        {
            throw new NotImplementedException();
        }

		public void Dispose()
		{
			_defaultAssemblyResolver.Dispose();
		}
	}
}