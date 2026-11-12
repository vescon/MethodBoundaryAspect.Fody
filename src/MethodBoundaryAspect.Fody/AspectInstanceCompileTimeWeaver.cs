using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MethodBoundaryAspect.Fody.Ordering;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// Generates the static class <c>OnMethodBoundaryAspectCompile.Aspects</c> with a field and a getter for each
    /// aspect which is shared by all calls, see <see cref="AspectInstanceReuseAnalyzer"/>. The aspect is created
    /// on the first call, like the method infos of <see cref="MethodInfoCompileTimeWeaver"/>.
    /// </summary>
    public class AspectInstanceCompileTimeWeaver
    {
        private const string CacheClass = "Aspects";

        private readonly ModuleDefinition _module;
        private readonly AspectInstanceReuseAnalyzer _analyzer;
        private TypeDefinition _aspectsClass;

        // the getters by attribute: looked up for every method an attribute (e.g. of a class) is applied to
        // (the key of the attribute is expensive)
        private readonly Dictionary<CustomAttribute, MethodDefinition> _getters = new Dictionary<CustomAttribute, MethodDefinition>();

        // one instance (and one field and getter) for all uses of an attribute with the same constructor, arguments,
        // properties and fields: [Log] on 1000 methods is one instance, not 1000. The reuse analysis guarantees that
        // the instances can't be told apart.
        private readonly Dictionary<string, MethodDefinition> _gettersByValue = new Dictionary<string, MethodDefinition>();

        public AspectInstanceCompileTimeWeaver(ModuleDefinition module, AspectInstanceReuseAnalyzer analyzer)
        {
            _module = module;
            _analyzer = analyzer;
        }

        /// <summary>
        /// The method returning the shared instance of the aspect, null if a new instance has to be created for every call.
        /// </summary>
        public MethodDefinition GetGetter(AspectInfo aspect)
        {
            var attribute = aspect.AspectAttribute;
            if (_getters.TryGetValue(attribute, out var getter))
                return getter;

            if (!_analyzer.CanReuse(aspect.AspectTypeDefinition)
                || attribute.AttributeType.ContainsGenericParameter
                || !IsAccessible(aspect.AspectTypeDefinition)
                || !IsAccessible(attribute.AttributeType)
                || !IsAccessible(attribute.Constructor))
                return null;

            var key = GetKey(attribute);
            if (!_gettersByValue.TryGetValue(key, out getter))
            {
                getter = AddGetter(attribute);
                _gettersByValue.Add(key, getter);
            }

            _getters[attribute] = getter;
            return getter;
        }

        // the aspect is created from this: the constructor, its arguments, the properties and the fields set in the attribute
        private static string GetKey(CustomAttribute attribute)
        {
            var key = new StringBuilder(attribute.Constructor.FullName);
            foreach (var argument in attribute.ConstructorArguments)
                key.Append('|').Append(GetKey(argument));

            foreach (var property in attribute.Properties.OrderBy(p => p.Name, StringComparer.Ordinal))
                key.Append("|P:").Append(property.Name).Append('=').Append(GetKey(property.Argument));

            foreach (var field in attribute.Fields.OrderBy(f => f.Name, StringComparer.Ordinal))
                key.Append("|F:").Append(field.Name).Append('=').Append(GetKey(field.Argument));

            return key.ToString();
        }

        private static string GetKey(CustomAttributeArgument argument)
        {
            var type = argument.Type.FullName;
            switch (argument.Value)
            {
                case null:
                    return type + ":null";
                case CustomAttributeArgument nested: // a value of the type object
                    return type + ":(" + GetKey(nested) + ")";
                case CustomAttributeArgument[] array:
                    return type + ":[" + string.Join(",", array.Select(GetKey)) + "]";
                case TypeReference typeReference:
                    return type + ":typeof(" + typeReference.FullName + ")";
                case string text:
                    return type + ":\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                default:
                    // the CLR type tells 1 (int) from 1L (long), double and float use the round-trip format
                    return type + ":" + argument.Value.GetType().Name + " " + (argument.Value is IFormattable formattable ? formattable.ToString(argument.Value is float || argument.Value is double ? "R" : null, CultureInfo.InvariantCulture) : argument.Value.ToString());
            }
        }

        // the aspect is created in another class than the one of the woven method, e.g. not possible for a private nested aspect
        private bool IsAccessible(TypeReference type)
        {
            switch (type)
            {
                case GenericInstanceType generic:
                    return IsAccessible(generic.ElementType) && generic.GenericArguments.All(IsAccessible);
                case TypeSpecification specification:
                    return IsAccessible(specification.ElementType);
                case GenericParameter _:
                    return true;
            }

            var definition = type.Resolve();
            if (definition == null)
                return false;

            for (var current = definition; current != null; current = current.DeclaringType)
            {
                var visible = current.IsPublic || current.IsNestedPublic
                              || (current.Module == _module && (current.IsNotPublic || current.IsNestedAssembly || current.IsNestedFamilyOrAssembly));
                if (!visible)
                    return false;
            }

            return true;
        }

        private bool IsAccessible(TypeDefinition aspectType)
        {
            return IsAccessible((TypeReference)aspectType);
        }

        // only the constructor which the attribute calls is needed, the aspect can have private ones as well
        private bool IsAccessible(MethodReference constructor)
        {
            var definition = constructor.Resolve();
            if (definition == null)
                return false;

            return definition.IsPublic
                   || (definition.Module == _module && (definition.IsAssembly || definition.IsFamilyOrAssembly));
        }

        private MethodDefinition AddGetter(CustomAttribute attribute)
        {
            if (_aspectsClass == null)
                _aspectsClass = CompileTimeClassBuilder.CreateStaticClass(_module, CacheClass, _module.TypeSystem.Object);

            var aspectType = _module.ImportReference(attribute.AttributeType);
            var identifier = $"{_gettersByValue.Count}_{attribute.AttributeType.Name}";

            var field = new FieldDefinition($"_aspect_{identifier}", FieldAttributes.Private | FieldAttributes.Static, aspectType);
            _aspectsClass.Fields.Add(field);

            var getter = new MethodDefinition($"GetAspect_{identifier}",
                MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig,
                aspectType)
            {
                AggressiveInlining = true
            };
            // PEVerify wants initlocals for a method with a local variable
            getter.Body.InitLocals = true;
            _aspectsClass.Methods.Add(getter);

            // var aspect = _aspect; if (aspect == null) _aspect = aspect = new Aspect(...);
            // concurrent first calls may both create it, which is harmless for a reusable aspect
            var processor = getter.Body.GetILProcessor();
            CompileTimeClassBuilder.AppendLazyGetterBody(processor, field, () =>
            {
                var newAspect = new InstructionBlockChainCreator(getter, _module).CreateAndNewUpAspect(attribute);
                newAspect.Append(processor);
                processor.Append(Instruction.Create(OpCodes.Ldloc, newAspect.Variable));
            });

            return getter;
        }
    }
}
