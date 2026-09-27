using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// Ref structs (e.g. Span&lt;T&gt;) cannot be boxed, so they cannot be passed to
    /// the aspect via MethodExecutionArgs. Weaving a box instruction for them results in an
    /// InvalidProgramException at runtime.
    /// see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/129
    /// </summary>
    public static class ByRefLikeExtensions
    {
        private const string IsByRefLikeAttributeFullName = "System.Runtime.CompilerServices.IsByRefLikeAttribute";

        // GenericParameterAttributes.AllowByRefLike ("allows ref struct"), not known by Mono.Cecil 0.11
        private const GenericParameterAttributes AllowByRefLike = (GenericParameterAttributes)0x0020;

        public static bool IsByRefLike(this TypeReference type)
        {
            // ref / out / in parameters and ref returns
            while (type is ByReferenceType byReferenceType)
                type = byReferenceType.ElementType;

            if (type is GenericParameter genericParameter)
                return (genericParameter.Attributes & AllowByRefLike) != 0;

            if (!type.IsValueType)
                return false;

            var typeDefinition = type.Resolve();
            return typeDefinition != null
                   && typeDefinition.CustomAttributes.Any(a => a.AttributeType.FullName == IsByRefLikeAttributeFullName);
        }

        public static IEnumerable<string> GetByRefLikeValueNames(this MethodDefinition method)
        {
            if (method.HasThis && method.DeclaringType.IsByRefLike())
                yield return "instance";

            foreach (var parameter in method.Parameters.Where(p => p.ParameterType.IsByRefLike()))
                yield return $"parameter '{parameter.Name}'";

            if (method.ReturnType.IsByRefLike())
                yield return "return value";
        }
    }
}
