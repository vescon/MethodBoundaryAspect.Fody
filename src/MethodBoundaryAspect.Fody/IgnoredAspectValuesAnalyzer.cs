using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// The weaver doesn't run the code of the aspects, it only reads the values set where an aspect is applied,
    /// e.g. [Log(NamespaceFilter = "...")]. A filter or visibility set in the code of the aspect is never seen,
    /// neither in its constructor nor by overriding the property: it is silently ignored.
    /// This finds these places, so the weaver can warn about them.
    /// </summary>
    public static class IgnoredAspectValuesAnalyzer
    {
        private static readonly string[] PropertyNames =
        {
            AttributeNames.AttributeTargetMemberAttributes,
            AttributeNames.NamespaceFilter,
            AttributeNames.TypeNameFilter,
            AttributeNames.MethodNameFilter
        };

        /// <summary>
        /// The names of the properties of <see cref="AttributeFullNames.OnMethodBoundaryAspect"/> the aspect sets in
        /// its code or in the code of its base aspects, sorted by name.
        /// </summary>
        public static IReadOnlyList<string> GetValuesSetInAspectCode(TypeDefinition aspectType)
        {
            var found = new SortedSet<string>();

            var hierarchy = GetAspectHierarchy(aspectType);
            foreach (var type in hierarchy)
            {
                // overridden properties, e.g. "public override string NamespaceFilter => ..."
                foreach (var method in type.Methods.Where(m => m.IsVirtual && !m.IsNewSlot))
                {
                    var propertyName = GetAccessedPropertyName(method.Name);
                    if (propertyName != null)
                        found.Add(propertyName);
                }

                // assignments in the constructors, e.g. "NamespaceFilter = ..."
                foreach (var constructor in type.Methods.Where(m => m.IsConstructor && !m.IsStatic && m.HasBody))
                {
                    foreach (var instruction in constructor.Body.Instructions)
                    {
                        if (!IsCall(instruction) || !(instruction.Operand is MethodReference called))
                            continue;

                        if (!called.Name.StartsWith("set_") || !IsOfHierarchy(called.DeclaringType, hierarchy))
                            continue;

                        var propertyName = GetAccessedPropertyName(called.Name);
                        if (propertyName != null)
                            found.Add(propertyName);
                    }
                }
            }

            return found.ToList();
        }

        /// <summary>
        /// The aspect and its base aspects, without OnMethodBoundaryAspect itself (which declares the properties).
        /// </summary>
        private static List<TypeDefinition> GetAspectHierarchy(TypeDefinition aspectType)
        {
            var hierarchy = new List<TypeDefinition>();
            for (var type = aspectType; type != null && type.FullName != AttributeFullNames.OnMethodBoundaryAspect; type = type.BaseType?.Resolve())
                hierarchy.Add(type);
            return hierarchy;
        }

        private static string GetAccessedPropertyName(string accessorName)
        {
            if (!accessorName.StartsWith("get_") && !accessorName.StartsWith("set_"))
                return null;

            var propertyName = accessorName.Substring("get_".Length);
            return PropertyNames.Contains(propertyName) ? propertyName : null;
        }

        private static bool IsCall(Instruction instruction) =>
            instruction.OpCode.Code == Code.Call || instruction.OpCode.Code == Code.Callvirt;

        // the setter is called on the aspect itself (this.X = ...), where the declaring type is the aspect, one of its
        // base aspects or OnMethodBoundaryAspect
        private static bool IsOfHierarchy(TypeReference declaringType, List<TypeDefinition> hierarchy) =>
            declaringType.FullName == AttributeFullNames.OnMethodBoundaryAspect
            || hierarchy.Any(t => t.FullName == declaringType.FullName);
    }
}
