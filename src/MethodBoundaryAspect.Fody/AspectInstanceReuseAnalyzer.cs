using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// Determines if one instance of an aspect can be shared by all calls of the woven methods, instead of creating
    /// a new instance for every call.
    /// The analysis is conservative, an aspect is only shared if it can't be told from a new instance:
    /// <list type="bullet">
    /// <item>it has no finalizer and only instance fields of immutable types (primitives, enums, string, Type)</item>
    /// <item>its constructors only store their arguments and constants in fields and call the base constructor,
    /// so creating it only once has no effect</item>
    /// <item>its properties are only set by the constructor call of the weaved code, the setters just store
    /// the value</item>
    /// <item>no method writes an instance field, takes the address of one, calls a setter or hands out "this"
    /// (except to call a method of the aspect, or GetType())</item>
    /// </list>
    /// An aspect which is not found to be reusable is created for every call as before.
    /// </summary>
    public class AspectInstanceReuseAnalyzer
    {
        private readonly Dictionary<TypeDefinition, bool> _cache = new Dictionary<TypeDefinition, bool>();
        private readonly HashSet<string> _disabledAspects;
        private readonly bool _disabled;

        public AspectInstanceReuseAnalyzer(bool disabled, IEnumerable<string> disabledAspects)
        {
            _disabled = disabled;
            _disabledAspects = new HashSet<string>(disabledAspects.Select(ExecutionArgsUsageAnalyzer.NormalizeTypeName));
        }

        /// <summary>
        /// The configured aspect names which matched at least one weaved aspect.
        /// </summary>
        public HashSet<string> MatchedDisabledAspects { get; } = new HashSet<string>();

        public bool CanReuse(TypeDefinition aspectType)
        {
            if (aspectType == null)
                return false;

            var aspectTypeName = ExecutionArgsUsageAnalyzer.NormalizeTypeName(aspectType.FullName);
            if (_disabledAspects.Contains(aspectTypeName))
            {
                MatchedDisabledAspects.Add(aspectTypeName);
                return false;
            }

            if (_disabled)
                return false;

            if (!_cache.TryGetValue(aspectType, out var canReuse))
            {
                canReuse = Analyze(aspectType);
                _cache.Add(aspectType, canReuse);
            }

            return canReuse;
        }

        private static bool Analyze(TypeDefinition aspectType)
        {
            var hierarchy = GetHierarchy(aspectType);
            if (hierarchy == null)
                return false;

            // the runtime base class is not analyzed, its state is only written by the setters of its properties
            return hierarchy
                .Where(t => t.FullName != AttributeFullNames.OnMethodBoundaryAspect)
                .All(t => IsReusable(t, hierarchy));
        }

        /// <summary>
        /// The aspect type and its base types up to OnMethodBoundaryAspect, null if a type can't be resolved.
        /// </summary>
        private static List<TypeDefinition> GetHierarchy(TypeDefinition aspectType)
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

        private static bool IsReusable(TypeDefinition type, List<TypeDefinition> hierarchy)
        {
            // no IL to analyze, e.g. the aspect was resolved from a reference assembly
            if (ExecutionArgsUsageAnalyzer.IsReferenceAssembly(type.Module))
                return false;

            if (type.Fields.Any(f => !f.IsStatic && !IsImmutable(f.FieldType)))
                return false;

            foreach (var method in type.Methods)
            {
                if (method.Name == "Finalize" && !method.HasParameters)
                    return false;

                if (!method.HasBody)
                {
                    if (method.IsAbstract)
                        continue;
                    return false; // extern
                }

                if (method.IsConstructor && !method.IsStatic)
                {
                    if (!IsTrivialConstructor(method, type))
                        return false;
                }
                else if (method.IsSetter && !method.IsStatic)
                {
                    if (!IsTrivialSetter(method))
                        return false;
                }
                else if (!IsStateless(method, hierarchy))
                    return false;
            }

            return true;
        }

        private static bool IsImmutable(TypeReference type)
        {
            return type.IsPrimitive
                   || type.MetadataType == MetadataType.String
                   || type.FullName == "System.Type"
                   || type.Resolve()?.IsEnum == true;
        }

        /// <summary>
        /// ldarg.0, [ldarg/ldc/ldnull/ldstr, stfld]*, call base constructor, ret. A constructor doing anything else
        /// (counting instances, reading a configuration, ...) would be called less often.
        /// </summary>
        private static bool IsTrivialConstructor(MethodDefinition constructor, TypeDefinition type)
        {
            var baseTypeName = type.BaseType?.FullName;
            foreach (var instruction in constructor.Body.Instructions)
            {
                var code = instruction.OpCode.Code;
                if (code == Code.Nop || code == Code.Ret || code == Code.Ldnull || code == Code.Ldstr)
                    continue;

                var name = instruction.OpCode.Name;
                if (name.StartsWith("ldc.") || (name.StartsWith("ldarg") && !name.StartsWith("ldarga")))
                    continue;

                if (code == Code.Stfld && instruction.Operand is FieldReference field && field.Resolve()?.DeclaringType == type)
                    continue;

                if (code == Code.Call
                    && instruction.Operand is MethodReference baseConstructor
                    && baseConstructor.Name == ".ctor"
                    && baseConstructor.DeclaringType.FullName == baseTypeName)
                    continue;

                return false;
            }

            return true;
        }

        /// <summary>
        /// ldarg.0, ldarg.1, stfld, ret
        /// </summary>
        private static bool IsTrivialSetter(MethodDefinition setter)
        {
            var codes = setter.Body.Instructions
                .Where(i => i.OpCode.Code != Code.Nop)
                .Select(i => i.OpCode.Code)
                .ToList();

            return setter.Body.Instructions.Count(i => i.OpCode.Code == Code.Stfld) == 1
                   && codes.SequenceEqual(new[] { Code.Ldarg_0, Code.Ldarg_1, Code.Stfld, Code.Ret });
        }

        private static bool IsStateless(MethodDefinition method, List<TypeDefinition> hierarchy)
        {
            foreach (var instruction in method.Body.Instructions)
            {
                switch (instruction.OpCode.Code)
                {
                    case Code.Stfld:
                    case Code.Ldflda:
                    case Code.Calli:
                        return false;
                }

                if (instruction.Operand is MethodReference calledMethod && instruction.OpCode.Code != Code.Ldftn)
                {
                    var called = calledMethod.Resolve();
                    if (called != null && called.IsSetter && called.HasThis && IsOfHierarchy(called.DeclaringType, hierarchy))
                        return false;
                }

                if (method.HasThis && !IsThisAllowed(instruction, method, hierarchy))
                    return false;
            }

            return true;
        }

        private static bool IsThisAllowed(Instruction instruction, MethodDefinition method, List<TypeDefinition> hierarchy)
        {
            var code = instruction.OpCode.Code;
            var isThis = code == Code.Ldarg_0
                         || ((code == Code.Ldarg || code == Code.Ldarg_S) && instruction.Operand == method.Body.ThisParameter);
            if (code == Code.Starg || code == Code.Starg_S || code == Code.Ldarga || code == Code.Ldarga_S)
                return instruction.Operand != method.Body.ThisParameter;

            if (!isThis)
                return true;

            var consumer = ExecutionArgsUsageAnalyzer.FindConsumer(instruction, out var argumentIndex);
            if (consumer == null || argumentIndex != 0)
                return false;

            if (consumer.OpCode.Code == Code.Ldfld)
                return true;

            if ((consumer.OpCode.Code != Code.Call && consumer.OpCode.Code != Code.Callvirt)
                || !(consumer.Operand is MethodReference calledMethod)
                || !calledMethod.HasThis)
                return false;

            // calling the methods of the aspect is fine, they are analyzed as well
            if (calledMethod.DeclaringType.FullName == "System.Object")
                return calledMethod.Name == "GetType";

            var called = calledMethod.Resolve();
            if (called == null || called.IsSetter)
                return false;

            if (called.DeclaringType.FullName == AttributeFullNames.OnMethodBoundaryAspect)
                return called.Name == AspectMethodCriteria.OnEntryMethodName
                       || called.Name == AspectMethodCriteria.OnExitMethodName
                       || called.Name == AspectMethodCriteria.OnExceptionMethodName
                       || called.IsGetter;

            return IsOfHierarchy(called.DeclaringType, hierarchy);
        }

        private static bool IsOfHierarchy(TypeDefinition type, List<TypeDefinition> hierarchy)
        {
            return hierarchy.Any(t => t.FullName == type.FullName);
        }
    }
}
