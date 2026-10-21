using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace MethodBoundaryAspect.Fody
{
    public static class AspectTypeName
    {
        /// <summary>
        /// The full name of an aspect as written in FodyWeavers.xml (nested types as "Namespace.Outer+Inner"),
        /// in the form of Cecil ("Namespace.Outer/Inner").
        /// </summary>
        public static string Normalize(string typeName) => typeName.Trim().Replace('+', '/');
    }

    /// <summary>
    /// The common part of the analyzers which decide by the IL of an aspect how it is woven: the result is determined
    /// once per aspect type, and can be switched off for all aspects or for single aspects in FodyWeavers.xml.
    /// </summary>
    /// <typeparam name="TResult">The result of the analysis.</typeparam>
    public abstract class AspectAnalyzerBase<TResult>
    {
        private const string ReferenceAssemblyAttributeName = "System.Runtime.CompilerServices.ReferenceAssemblyAttribute";

        private readonly Dictionary<TypeDefinition, TResult> _cache = new Dictionary<TypeDefinition, TResult>();
        private readonly HashSet<string> _disabledAspects;
        private readonly bool _disabled;

        protected AspectAnalyzerBase(bool disabled, IEnumerable<string> disabledAspects)
        {
            _disabled = disabled;
            _disabledAspects = new HashSet<string>(disabledAspects.Select(AspectTypeName.Normalize));
        }

        /// <summary>
        /// The configured aspect names which matched at least one weaved aspect.
        /// </summary>
        public HashSet<string> MatchedDisabledAspects { get; } = new HashSet<string>();

        /// <summary>
        /// The result of the analysis of the aspect type, or <paramref name="resultIfDisabled"/> if the analysis
        /// is switched off for the aspect.
        /// </summary>
        protected TResult GetResult(TypeDefinition aspectType, TResult resultIfDisabled)
        {
            var aspectTypeName = AspectTypeName.Normalize(aspectType.FullName);
            if (_disabledAspects.Contains(aspectTypeName))
            {
                MatchedDisabledAspects.Add(aspectTypeName);
                return resultIfDisabled;
            }

            if (_disabled)
                return resultIfDisabled;

            if (!_cache.TryGetValue(aspectType, out var result))
            {
                result = Analyze(aspectType);
                _cache.Add(aspectType, result);
            }

            return result;
        }

        protected abstract TResult Analyze(TypeDefinition aspectType);

        /// <summary>
        /// The aspect type and its base types up to OnMethodBoundaryAspect, null if a type can't be resolved.
        /// </summary>
        protected static List<TypeDefinition> GetHierarchy(TypeDefinition aspectType)
        {
            var hierarchy = new List<TypeDefinition>();
            var currentType = aspectType;
            while (true)
            {
                hierarchy.Add(currentType);
                if (currentType.FullName == AttributeFullNames.OnMethodBoundaryAspect)
                    return hierarchy;

                currentType = currentType.BaseType?.Resolve();
                if (currentType == null)
                    return null;
            }
        }

        protected static bool IsReferenceAssembly(ModuleDefinition module)
        {
            return module.Assembly.CustomAttributes.Any(a => a.AttributeType.FullName == ReferenceAssemblyAttributeName);
        }
    }
}
