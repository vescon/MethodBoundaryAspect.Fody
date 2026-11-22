using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// An extension block (C# 14) is compiled to static methods of the class ("implementation methods", they are woven)
    /// and to a nested "grouping type" named "&lt;G&gt;$..." which describes the members for the compiler: a copy of each
    /// member with its attributes and a body that only throws. The properties exist only in the grouping type, so the
    /// aspects of an extension property are read from there.
    /// </summary>
    internal static class ExtensionBlocks
    {
        private const string MarkerAttributeName = "System.Runtime.CompilerServices.ExtensionMarkerAttribute";
        private const string MarkerMethodName = "<Extension>$";

        public static bool IsGroupingType(TypeDefinition type)
        {
            return type.IsNested && type.IsSpecialName && type.Name.StartsWith("<G>$");
        }

        /// <summary>The implementation methods of the accessors of the extension properties of a class.</summary>
        public static Dictionary<MethodDefinition, PropertyDefinition> GetPropertiesOfImplementationMethods(TypeDefinition type)
        {
            var result = new Dictionary<MethodDefinition, PropertyDefinition>();
            if (!type.HasNestedTypes)
                return result;

            foreach (var groupingType in type.NestedTypes.Where(IsGroupingType))
            {
                foreach (var property in groupingType.Properties)
                {
                    foreach (var accessor in new[] { property.GetMethod, property.SetMethod }.Where(x => x != null))
                    {
                        var implementation = FindImplementationMethod(type, groupingType, property, accessor);
                        if (implementation != null && !result.ContainsKey(implementation))
                            result.Add(implementation, property);
                    }
                }
            }

            return result;
        }

        // Same name, the receiver as first parameter (not for static members) and the type parameters of the block
        // as type parameters of the method. If it is not found exactly, the property aspects are not applied.
        private static MethodDefinition FindImplementationMethod(
            TypeDefinition type,
            TypeDefinition groupingType,
            PropertyDefinition property,
            MethodDefinition accessor)
        {
            var signature = new List<string>();
            if (!accessor.IsStatic)
            {
                var receiverType = GetReceiverType(groupingType, property, accessor);
                if (receiverType == null)
                    return null;

                signature.Add(Describe(receiverType));
            }

            signature.AddRange(accessor.Parameters.Select(p => Describe(p.ParameterType)));

            var candidates = type.Methods
                .Where(m => m.IsStatic
                            && m.Name == accessor.Name
                            && m.GenericParameters.Count == groupingType.GenericParameters.Count
                            && m.Parameters.Select(p => Describe(p.ParameterType)).SequenceEqual(signature))
                .ToList();
            return candidates.Count == 1 ? candidates[0] : null;
        }

        // the member names its "marker type" (nested in the grouping type), whose method "<Extension>$" has the
        // receiver as parameter
        private static TypeReference GetReceiverType(TypeDefinition groupingType, PropertyDefinition property, MethodDefinition accessor)
        {
            var markerAttribute = accessor.CustomAttributes.Concat(property.CustomAttributes)
                .FirstOrDefault(a => a.AttributeType.FullName == MarkerAttributeName);
            if (markerAttribute == null || markerAttribute.ConstructorArguments.Count != 1)
                return null;

            var markerTypeName = markerAttribute.ConstructorArguments[0].Value as string;
            var markerMethod = groupingType.NestedTypes
                .FirstOrDefault(t => t.Name == markerTypeName)
                ?.Methods.FirstOrDefault(m => m.Name == MarkerMethodName && m.Parameters.Count == 1);
            return markerMethod?.Parameters[0].ParameterType;
        }

        // Type parameters by position: the grouping type declares them as its own (with other names), the
        // implementation method as method type parameters.
        private static string Describe(TypeReference type)
        {
            switch (type)
            {
                case GenericParameter genericParameter:
                    return "!" + genericParameter.Position;
                case ByReferenceType byReferenceType:
                    return Describe(byReferenceType.ElementType) + "&";
                case PointerType pointerType:
                    return Describe(pointerType.ElementType) + "*";
                case ArrayType arrayType:
                    return Describe(arrayType.ElementType) + "[" + new string(',', arrayType.Rank - 1) + "]";
                case GenericInstanceType genericInstanceType:
                    return genericInstanceType.ElementType.FullName
                           + "<" + string.Join(",", genericInstanceType.GenericArguments.Select(Describe)) + ">";
                case RequiredModifierType requiredModifierType:
                    return Describe(requiredModifierType.ElementType);
                case OptionalModifierType optionalModifierType:
                    return Describe(optionalModifierType.ElementType);
                default:
                    return type.FullName;
            }
        }
    }
}
