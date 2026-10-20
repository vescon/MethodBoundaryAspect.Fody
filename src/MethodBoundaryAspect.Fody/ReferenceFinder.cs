using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Linq;

namespace MethodBoundaryAspect.Fody
{
    public class ReferenceFinder
    {
        private readonly ModuleDefinition _moduleDefinition;
        private readonly CoreLibraryReflectionImporter _reflectionImporter;

        public ReferenceFinder(ModuleDefinition moduleDefinition)
        {
            _moduleDefinition = moduleDefinition;
            _reflectionImporter = new CoreLibraryReflectionImporter(moduleDefinition);
        }

        public MethodReference GetMethodReference(Type declaringType, Func<MethodDefinition, bool> predicate)
        {
            return GetMethodReference(GetTypeReference(declaringType), predicate);
        }

        public MethodReference GetMethodReference(TypeReference typeReference, Func<MethodDefinition, bool> predicate)
        {
            var currentType = typeReference;

            MethodDefinition methodDefinition = null;
            while (currentType != null)
            {
                var typeDefinition = currentType.Resolve();
                if (typeDefinition == null)
                    break;

                methodDefinition = typeDefinition.Methods.FirstOrDefault(predicate);
                if (methodDefinition != null)
                    break;

                currentType = GetBaseType(currentType, typeDefinition);
            }

            if (methodDefinition == null)
                throw new InvalidOperationException($"Didn't find the method on type '{typeReference.FullName}' or its base types.");

            return ImportMethod(methodDefinition, currentType);
        }

        /// <summary>
        /// A reference to the method as a member of <paramref name="declaringType"/>. For a method of a generic type
        /// the declaring type has to be the instantiated type (e.g. of a generic aspect), the reference to the
        /// method of the open generic type cannot be called.
        /// </summary>
        public MethodReference ImportMethod(MethodDefinition method, TypeReference declaringType)
        {
            if (!(declaringType is GenericInstanceType))
                return _moduleDefinition.ImportReference(method);

            var reference = new MethodReference(method.Name, ImportSignatureType(method.ReturnType), declaringType)
            {
                HasThis = method.HasThis,
                ExplicitThis = method.ExplicitThis,
                CallingConvention = method.CallingConvention
            };
            foreach (var parameter in method.Parameters)
                reference.Parameters.Add(new ParameterDefinition(ImportSignatureType(parameter.ParameterType)));
            foreach (var genericParameter in method.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(genericParameter.Name, reference));

            return reference;
        }

        public FieldReference ImportField(FieldDefinition field, TypeReference declaringType)
        {
            return declaringType is GenericInstanceType
                ? new FieldReference(field.Name, ImportSignatureType(field.FieldType), declaringType)
                : _moduleDefinition.ImportReference(field);
        }

        /// <summary>
        /// The base type of <paramref name="type"/>, with the generic arguments of the type applied
        /// (e.g. <c>Base&lt;string&gt;</c> for <c>Derived&lt;string&gt; : Base&lt;T&gt;</c>).
        /// </summary>
        public static TypeReference GetBaseType(TypeReference type, TypeDefinition typeDefinition)
        {
            var baseType = typeDefinition.BaseType;
            if (!(baseType is GenericInstanceType baseInstance) || !(type is GenericInstanceType typeInstance))
                return baseType;

            var result = new GenericInstanceType(baseInstance.ElementType);
            foreach (var argument in baseInstance.GenericArguments)
            {
                result.GenericArguments.Add(argument is GenericParameter parameter
                                            && parameter.Type == GenericParameterType.Type
                                            && parameter.Position < typeInstance.GenericArguments.Count
                    ? typeInstance.GenericArguments[parameter.Position]
                    : argument);
            }

            return result;
        }

        // generic parameters of the declaring type stay as they are, the importer cannot import them without a context
        private TypeReference ImportSignatureType(TypeReference type)
        {
            return type.ContainsGenericParameter ? type : _moduleDefinition.ImportReference(type);
        }

        public MethodReference GetConstructorReference(TypeReference typeReference, Func<MethodDefinition, bool> predicate)
        {
            var typeDefinition = typeReference.Resolve();
            var methodDefinition = typeDefinition.GetConstructors().FirstOrDefault(predicate)
                ?? throw new InvalidOperationException($"Didn't find the constructor on type '{typeReference.FullName}'.");
            return _moduleDefinition.ImportReference(methodDefinition);
        }

        public TypeReference GetTypeReference(Type type, string netCoreAssemblyHint = null)
        {
            var importedType = _reflectionImporter.ImportReference(type, null);
            // On .NET Core, we need to rewrite mscorlib types to use the
            // dot net assemblies from the weaved assembly and not the ones
            // used by the weaver itself.
            if (importedType is TypeSpecification)
                return importedType;

            var scope = importedType.Scope;
            if (scope.Name != _moduleDefinition.TypeSystem.CoreLibrary.Name)
                scope = _moduleDefinition.TypeSystem.CoreLibrary;

            if (scope.Name == "System.Runtime" && netCoreAssemblyHint != null)
                scope = new AssemblyNameReference(netCoreAssemblyHint,
                    _moduleDefinition.AssemblyReferences.First(mr => mr.Name == "System.Runtime").Version);

            importedType.Scope = scope;
            return importedType;
        }
    }
}