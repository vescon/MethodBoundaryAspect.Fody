using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Linq;

namespace MethodBoundaryAspect.Fody
{
    public class ReferenceFinder
    {
        private readonly ModuleDefinition _moduleDefinition;

        public ReferenceFinder(ModuleDefinition moduleDefinition)
        {
            _moduleDefinition = moduleDefinition;
        }

        public MethodReference GetMethodReference(Type declaringType, Func<MethodDefinition, bool> predicate)
        {
            return GetMethodReference(GetTypeReference(declaringType), predicate);
        }

        public MethodReference GetMethodReference(TypeReference typeReference, Func<MethodDefinition, bool> predicate)
        {
            var typeDefinition = typeReference.Resolve();

            MethodDefinition methodDefinition;
            do
            {
                methodDefinition = typeDefinition.Methods.FirstOrDefault(predicate);
                typeDefinition = typeDefinition.BaseType?.Resolve();
            } while (methodDefinition == null && typeDefinition != null);

            return _moduleDefinition.ImportReference(methodDefinition);
        }

        public MethodReference GetConstructorReference(TypeReference typeReference, Func<MethodDefinition, bool> predicate)
        {
            var typeDefinition = typeReference.Resolve();
            var methodDefinition = typeDefinition.GetConstructors().FirstOrDefault(predicate);
            return _moduleDefinition.ImportReference(methodDefinition);
        }

        public TypeReference GetTypeReference(Type type, string assemblyHint = null)
        {
            var importedType = _moduleDefinition.ImportReference(type);
            // On .NET Core, we need to rewrite mscorlib types to use the
            // dot net assemblies from the weaved assembly and not the ones
            // used by the weaver itself.
            string scopeName = importedType.Scope.Name;
            if (!(importedType is TypeSpecification) && scopeName == "System.Private.CoreLib")
            {
                IMetadataScope scope;

                if (assemblyHint == null)
                    scope = _moduleDefinition.TypeSystem.CoreLibrary;
                else
                    scope = new AssemblyNameReference(assemblyHint, _moduleDefinition.AssemblyReferences.First(mr => mr.Name == "System.Runtime").Version);

                importedType.Scope = scope;
            }
            return importedType;
        }
    }
}